using UnityEngine;

namespace BoneSender
{
    public class SenderBoneParseData : AndroidParseData
    {
        private const int m_DefaultFrameStorageSize = 100;
        private const int m_FixedSlotCount = 4;
        private const int m_RectElementCount = 4;
        private const float m_MinTrackedScore = 0.6f;

        [Header("界面槽位到 SDK 槽位的绑定，按界面从左到右对应 1 到 4 号槽位")]
        [Range(0, 3)]
        public int m_UiSlot1SdkSlot;

        [Range(0, 3)]
        public int m_UiSlot2SdkSlot = 1;

        [Range(0, 3)]
        public int m_UiSlot3SdkSlot = 2;

        [Range(0, 3)]
        public int m_UiSlot4SdkSlot = 3;

        private readonly BoneFrameAssembler m_FrameAssembler = new BoneFrameAssembler();
        private readonly SimulatedBoneFrameGenerator m_SimulatedFrameGenerator = new SimulatedBoneFrameGenerator();
        private int[] m_CurrentFramePersonIds = new int[0];
        private readonly int[] m_SlotPersonIds = new int[m_FixedSlotCount];
        private readonly int[] m_ExcludedPersonIds = new int[m_FixedSlotCount];
        private readonly float[,] m_SlotRects = new float[m_FixedSlotCount, m_RectElementCount];
        private readonly int[] m_EffectiveUiSlotToSdkSlot = new int[m_FixedSlotCount];
        private int m_FrameSerialCounter;
        private bool m_IsFrameStorageInitialized;
        private string m_SessionId;
        private bool m_HasLoggedFirstValidFrame;
        private bool m_HadTrackedPersonsLastFrame;
        private int m_LastTrackedPersonCount;
        private int m_LastFirstTrackedSlotIndex = -1;
        private int m_LastFirstPersonId = YouDooSDKConstants.PersonIdNull;
        private bool m_HasInitializedSlotLayout;
        private bool m_HasLoggedInvalidUiSlotBinding;

        private BoneProtocolFrame m_LatestFrame;

        public BoneProtocolFrame ReadLatestFrame()
        {
            return m_LatestFrame;
        }

        public int ReadBoundSdkSlotIndex(int uiSlotIndex)
        {
            RefreshEffectiveUiSlotBinding(true);
            if (uiSlotIndex < 0 || uiSlotIndex >= m_FixedSlotCount)
            {
                return -1;
            }

            return m_EffectiveUiSlotToSdkSlot[uiSlotIndex];
        }

        public string ReadUiSlotBindingSummary()
        {
            RefreshEffectiveUiSlotBinding(true);
            return string.Format(
                "界面1<-SDK{0} | 界面2<-SDK{1} | 界面3<-SDK{2} | 界面4<-SDK{3}",
                m_EffectiveUiSlotToSdkSlot[0] + 1,
                m_EffectiveUiSlotToSdkSlot[1] + 1,
                m_EffectiveUiSlotToSdkSlot[2] + 1,
                m_EffectiveUiSlotToSdkSlot[3] + 1);
        }

        public string ReadSessionId()
        {
            return m_SessionId ?? string.Empty;
        }

        public int ReadLatestFrameSerial()
        {
            return m_LatestFrame != null ? m_LatestFrame.m_FrameSerial : m_FrameSerialCounter;
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(m_SessionId))
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                m_SessionId = string.IsNullOrEmpty(deviceId) ? System.Guid.NewGuid().ToString("N") : deviceId;
            }

            BoneSenderAppLogger.BindSessionIdReader(ReadSessionId);
            InitializeSlotLayout();
            RefreshEffectiveUiSlotBinding(true);
            BoneSenderAppLogger.Log("当前界面槽位绑定: " + ReadUiSlotBindingSummary());
        }

        private void Update()
        {
            if (ReadShouldUseSimulatedRuntime())
            {
                UpdateSimulatedFrame();
                return;
            }

            if (!ReadIsSdkRuntimeReady())
            {
                m_LatestFrame = null;
                return;
            }

            EnsureFrameStorageInitialized();
            UpdateRealFrame();
        }

        public override void UpdateGetPersonData()
        {
            UpdateRealFrame();
        }

        private void UpdateRealFrame()
        {
            InitializeSlotLayout();
            long frameInfoPtr = GetLatestFrameInfoPtr();
            if (frameInfoPtr == 0)
            {
                m_LatestFrame = null;
                return;
            }

            GetFrameInfoBasicByType(
                frameInfoPtr,
                (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
                out long frameTimeMs,
                out int imageWidth,
                out int imageHeight,
                out long _,
                out int objectCount);

            EnsurePersonIdBuffer(objectCount);
            int personCount = objectCount > 0
                ? GetPersonIdsByType(
                    frameInfoPtr,
                    (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
                    m_CurrentFramePersonIds,
                    m_CurrentFramePersonIds.Length)
                : 0;

            m_FrameAssembler.BeginFrame(
                m_SessionId,
                ++m_FrameSerialCounter,
                frameTimeMs,
                imageWidth,
                imageHeight);
            UpdateSlotAssignments(frameInfoPtr, m_CurrentFramePersonIds, personCount);
            TraversePersion(frameInfoPtr, m_SlotPersonIds, m_SlotPersonIds.Length);
            m_LatestFrame = RemapFrameToUiSlots(m_FrameAssembler.EndFrame());
            if (m_LatestFrame != null)
            {
                m_LatestFrame.m_IsSimulated = false;
            }

            UpdateTrackedPersonState(m_LatestFrame);
        }

        protected override void TraversePersion(long frameInfoPtr, int[] currentFramePersonIds, int perSonNumber)
        {
            for (int slotIndex = 0; slotIndex < perSonNumber; slotIndex++)
            {
                int personId = currentFramePersonIds[slotIndex];
                if (personId == YouDooSDKConstants.PersonIdNull)
                {
                    m_FrameAssembler.AddPerson(YouDooSDKConstants.PersonIdNull);
                    continue;
                }

                var bodyPart = TryCreatePart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON, personId);
                if (bodyPart == null)
                {
                    m_SlotPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
                    m_FrameAssembler.AddPerson(YouDooSDKConstants.PersonIdNull);
                    continue;
                }

                var person = m_FrameAssembler.AddPerson(personId);
                person.m_Body = bodyPart;
                person.m_LeftHand = TryCreatePart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_LEFT_HAND, personId) ?? new BoneProtocolPart();
                person.m_RightHand = TryCreatePart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_RIGHT_HAND, personId) ?? new BoneProtocolPart();
                person.m_Face = TryCreatePart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_FACE, personId) ?? new BoneProtocolPart();
            }
        }

        private BoneProtocolPart TryCreatePart(long frameInfoPtr, YouDooSDKConstants.DetectType detectType, int objectId)
        {
            if (!GetDetectBoxInfoByType(
                    frameInfoPtr,
                    (int)detectType,
                    objectId,
                    out float left,
                    out float top,
                    out float right,
                    out float bottom,
                    out float score,
                    out int keypointCount,
                    out int type))
            {
                return null;
            }

            var part = m_FrameAssembler.CreatePart(Mathf.Max(0, keypointCount));
            part.m_Score = score;
            part.m_Type = type;
            part.m_Rect.m_IsTracked = right > left && bottom > top;
            part.m_Rect.m_Left = left;
            part.m_Rect.m_Top = top;
            part.m_Rect.m_Right = right;
            part.m_Rect.m_Bottom = bottom;

            bool hasTrackedJoint = false;
            int jointCount = part.m_Joints.Length;
            for (int i = 0; i < jointCount; i++)
            {
                if (!GetKeyPointByType(
                        frameInfoPtr,
                        (int)detectType,
                        objectId,
                        i,
                        out float x,
                        out float y,
                        out float z,
                        out float jointScore))
                {
                    continue;
                }

                part.m_Joints[i].m_IsTracked = true;
                part.m_Joints[i].m_X = x;
                part.m_Joints[i].m_Y = y;
                part.m_Joints[i].m_Z = z;
                part.m_Joints[i].m_Score = jointScore;
                hasTrackedJoint = true;
            }

            return part.m_Rect.m_IsTracked || part.m_Score > 0f || hasTrackedJoint ? part : null;
        }

        private void EnsurePersonIdBuffer(int count)
        {
            if (m_CurrentFramePersonIds.Length >= count)
            {
                return;
            }

            m_CurrentFramePersonIds = new int[Mathf.Max(4, count)];
        }

        private void EnsureFrameStorageInitialized()
        {
            if (m_IsFrameStorageInitialized)
            {
                return;
            }

            InitFrameInfoStorage(m_DefaultFrameStorageSize);
            m_IsFrameStorageInitialized = true;
        }

        private void UpdateSimulatedFrame()
        {
            m_LatestFrame = RemapFrameToUiSlots(
                m_SimulatedFrameGenerator.BuildFrame(
                    m_SessionId,
                    ++m_FrameSerialCounter,
                    Time.unscaledTime));
        }

        private void UpdateTrackedPersonState(BoneProtocolFrame frame)
        {
            int trackedPersonCount = ReadTrackedPersonCount(frame);
            bool hasTrackedPersons = trackedPersonCount > 0;
            int firstTrackedSlotIndex = -1;
            int firstPersonId = YouDooSDKConstants.PersonIdNull;
            TryReadFirstTrackedPerson(frame, out firstTrackedSlotIndex, out firstPersonId);

            if (hasTrackedPersons && !m_HasLoggedFirstValidFrame)
            {
                m_HasLoggedFirstValidFrame = true;
                BoneSenderAppLogger.Log(
                    "首次收到有效人体帧，有效人数=" + trackedPersonCount +
                    "，首个有效槽位=" + ReadSlotDisplayIndex(firstTrackedSlotIndex) +
                    "，人物标识=" + firstPersonId);
            }
            else if (!m_HadTrackedPersonsLastFrame && hasTrackedPersons)
            {
                BoneSenderAppLogger.Log(
                    "已重新检测到人体，有效人数=" + trackedPersonCount +
                    "，首个有效槽位=" + ReadSlotDisplayIndex(firstTrackedSlotIndex) +
                    "，人物标识=" + firstPersonId);
            }
            else if (m_HadTrackedPersonsLastFrame && !hasTrackedPersons)
            {
                BoneSenderAppLogger.LogWarning(
                    "当前未检测到人体，上一帧有效人数=" + m_LastTrackedPersonCount +
                    "，上一帧首个有效槽位=" + ReadSlotDisplayIndex(m_LastFirstTrackedSlotIndex) +
                    "，人物标识=" + m_LastFirstPersonId);
            }

            m_HadTrackedPersonsLastFrame = hasTrackedPersons;
            m_LastTrackedPersonCount = trackedPersonCount;
            m_LastFirstTrackedSlotIndex = firstTrackedSlotIndex;
            m_LastFirstPersonId = firstPersonId;
        }

        private void InitializeSlotLayout()
        {
            if (m_HasInitializedSlotLayout)
            {
                return;
            }

            for (int slotIndex = 0; slotIndex < m_FixedSlotCount; slotIndex++)
            {
                m_SlotPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
            }

            SetSlotRect(0, 0.10f, 0.00f, 0.375f, 0.90f);
            SetSlotRect(1, 0.375f, 0.00f, 0.50f, 0.90f);
            SetSlotRect(2, 0.50f, 0.00f, 0.625f, 0.90f);
            SetSlotRect(3, 0.625f, 0.00f, 1.00f, 0.90f);

            m_HasInitializedSlotLayout = true;
        }

        private void SetSlotRect(int slotIndex, float left, float top, float right, float bottom)
        {
            m_SlotRects[slotIndex, 0] = left;
            m_SlotRects[slotIndex, 1] = top;
            m_SlotRects[slotIndex, 2] = right;
            m_SlotRects[slotIndex, 3] = bottom;
        }

        private BoneProtocolFrame RemapFrameToUiSlots(BoneProtocolFrame sourceFrame)
        {
            if (sourceFrame == null || sourceFrame.m_Persons == null || sourceFrame.m_Persons.Length <= 0)
            {
                return sourceFrame;
            }

            RefreshEffectiveUiSlotBinding(true);

            var remappedPersons = new BoneProtocolPerson[sourceFrame.m_Persons.Length];
            for (int uiSlotIndex = 0; uiSlotIndex < remappedPersons.Length; uiSlotIndex++)
            {
                int sdkSlotIndex = uiSlotIndex < m_FixedSlotCount ? m_EffectiveUiSlotToSdkSlot[uiSlotIndex] : uiSlotIndex;
                remappedPersons[uiSlotIndex] =
                    sdkSlotIndex >= 0 && sdkSlotIndex < sourceFrame.m_Persons.Length
                        ? sourceFrame.m_Persons[sdkSlotIndex]
                        : null;
            }

            sourceFrame.m_Persons = remappedPersons;
            return sourceFrame;
        }

        private void RefreshEffectiveUiSlotBinding(bool shouldLogWarning)
        {
            int slot0 = m_UiSlot1SdkSlot;
            int slot1 = m_UiSlot2SdkSlot;
            int slot2 = m_UiSlot3SdkSlot;
            int slot3 = m_UiSlot4SdkSlot;

            bool isValid =
                IsValidSdkSlotIndex(slot0) &&
                IsValidSdkSlotIndex(slot1) &&
                IsValidSdkSlotIndex(slot2) &&
                IsValidSdkSlotIndex(slot3) &&
                slot0 != slot1 &&
                slot0 != slot2 &&
                slot0 != slot3 &&
                slot1 != slot2 &&
                slot1 != slot3 &&
                slot2 != slot3;

            if (isValid)
            {
                m_EffectiveUiSlotToSdkSlot[0] = slot0;
                m_EffectiveUiSlotToSdkSlot[1] = slot1;
                m_EffectiveUiSlotToSdkSlot[2] = slot2;
                m_EffectiveUiSlotToSdkSlot[3] = slot3;
                return;
            }

            m_EffectiveUiSlotToSdkSlot[0] = 0;
            m_EffectiveUiSlotToSdkSlot[1] = 1;
            m_EffectiveUiSlotToSdkSlot[2] = 2;
            m_EffectiveUiSlotToSdkSlot[3] = 3;

            if (shouldLogWarning && !m_HasLoggedInvalidUiSlotBinding)
            {
                m_HasLoggedInvalidUiSlotBinding = true;
                BoneSenderAppLogger.LogWarning(
                    string.Format(
                        "界面槽位绑定配置无效，已回退为默认顺序。当前配置为 界面1<-SDK{0}，界面2<-SDK{1}，界面3<-SDK{2}，界面4<-SDK{3}",
                        slot0 + 1,
                        slot1 + 1,
                        slot2 + 1,
                        slot3 + 1));
            }
        }

        private static bool IsValidSdkSlotIndex(int sdkSlotIndex)
        {
            return sdkSlotIndex >= 0 && sdkSlotIndex < m_FixedSlotCount;
        }

        private void UpdateSlotAssignments(long frameInfoPtr, int[] currentFramePersonIds, int personCount)
        {
            int excludeCount = 0;
            for (int slotIndex = 0; slotIndex < m_FixedSlotCount; slotIndex++)
            {
                int personId = m_SlotPersonIds[slotIndex];
                if (personId == YouDooSDKConstants.PersonIdNull)
                {
                    continue;
                }

                if (!ReadContainsPersonId(currentFramePersonIds, personCount, personId) ||
                    !ReadIsPersonInsideSlot(frameInfoPtr, personId, slotIndex) ||
                    !TryReadPersonScore(frameInfoPtr, personId, out float score) ||
                    score < m_MinTrackedScore)
                {
                    m_SlotPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
                    continue;
                }

                m_ExcludedPersonIds[excludeCount++] = personId;
            }

            for (int slotIndex = 0; slotIndex < m_FixedSlotCount; slotIndex++)
            {
                if (m_SlotPersonIds[slotIndex] != YouDooSDKConstants.PersonIdNull)
                {
                    continue;
                }

                int foundPersonId = CheckPersonScoreInArea(
                    0,
                    m_SlotRects[slotIndex, 0],
                    m_SlotRects[slotIndex, 1],
                    m_SlotRects[slotIndex, 2],
                    m_SlotRects[slotIndex, 3],
                    m_ExcludedPersonIds,
                    excludeCount);
                if (foundPersonId == YouDooSDKConstants.PersonIdNull ||
                    !TryReadPersonScore(frameInfoPtr, foundPersonId, out float score) ||
                    score < m_MinTrackedScore)
                {
                    continue;
                }

                m_SlotPersonIds[slotIndex] = foundPersonId;
                if (excludeCount < m_ExcludedPersonIds.Length)
                {
                    m_ExcludedPersonIds[excludeCount++] = foundPersonId;
                }
            }
        }

        private static bool ReadContainsPersonId(int[] personIds, int personCount, int personId)
        {
            if (personIds == null || personId == YouDooSDKConstants.PersonIdNull)
            {
                return false;
            }

            for (int i = 0; i < personCount; i++)
            {
                if (personIds[i] == personId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ReadIsPersonInsideSlot(long frameInfoPtr, int personId, int slotIndex)
        {
            if (!TryReadPersonAnchorX(frameInfoPtr, personId, out float anchorX))
            {
                return false;
            }

            return anchorX >= m_SlotRects[slotIndex, 0] && anchorX <= m_SlotRects[slotIndex, 2];
        }

        private static bool TryReadPersonScore(long frameInfoPtr, int personId, out float score)
        {
            if (GetDetectBoxInfoByType(
                    frameInfoPtr,
                    (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
                    personId,
                    out _,
                    out _,
                    out _,
                    out _,
                    out score,
                    out _,
                    out _))
            {
                return true;
            }

            score = 0f;
            return false;
        }

        private static bool TryReadPersonAnchorX(long frameInfoPtr, int personId, out float anchorX)
        {
            if (GetKeyPointByType(
                    frameInfoPtr,
                    (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
                    personId,
                    (int)YouDooSDKConstants.KeyPointIndex.Nose,
                    out float x,
                    out _,
                    out _,
                    out _))
            {
                anchorX = x;
                return true;
            }

            if (GetDetectBoxInfoByType(
                    frameInfoPtr,
                    (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
                    personId,
                    out float left,
                    out _,
                    out float right,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                anchorX = (left + right) * 0.5f;
                return true;
            }

            anchorX = 0f;
            return false;
        }

        private static int ReadTrackedPersonCount(BoneProtocolFrame frame)
        {
            if (frame == null || frame.m_Persons == null)
            {
                return 0;
            }

            int trackedCount = 0;
            for (int i = 0; i < frame.m_Persons.Length; i++)
            {
                if (IsTrackedPerson(frame.m_Persons[i]))
                {
                    trackedCount++;
                }
            }

            return trackedCount;
        }

        private static bool TryReadFirstTrackedPerson(BoneProtocolFrame frame, out int slotIndex, out int personId)
        {
            if (frame != null && frame.m_Persons != null)
            {
                for (int i = 0; i < frame.m_Persons.Length; i++)
                {
                    BoneProtocolPerson person = frame.m_Persons[i];
                    if (!IsTrackedPerson(person))
                    {
                        continue;
                    }

                    slotIndex = i;
                    personId = person.m_PersonId;
                    return true;
                }
            }

            slotIndex = -1;
            personId = YouDooSDKConstants.PersonIdNull;
            return false;
        }

        private static bool IsTrackedPerson(BoneProtocolPerson person)
        {
            return person != null && person.m_PersonId != YouDooSDKConstants.PersonIdNull;
        }

        private static int ReadSlotDisplayIndex(int slotIndex)
        {
            return slotIndex >= 0 ? slotIndex + 1 : 0;
        }

        private static bool ReadShouldUseSimulatedRuntime()
        {
            return Application.isEditor || Application.platform != RuntimePlatform.Android;
        }

        private static bool ReadIsSdkRuntimeReady()
        {
            if (Application.isEditor || Application.platform != RuntimePlatform.Android)
            {
                return false;
            }

            BoneSenderAndroidServerInfo serverInfo = BoneSenderAndroidServerInfo.Current;
            return serverInfo != null && serverInfo.ReadIsFrameStreamReady();
        }
    }
}
