using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public class BattleBoneParseData : AndroidParseData, IBoneFrameSource
    {
        private const int m_DefaultFrameStorageSize = 100;
        private const int m_RectElementCount = 4;
        private const float m_MinTrackedScore = 0.6f;
        private const int m_MissingFrameThreshold = 30;

        private readonly BoneFrameData m_LatestFrameData = new BoneFrameData();
        private readonly Queue<BonePersonData> m_PersonPool = new Queue<BonePersonData>();
        private int[] m_CurrentFramePersonIds = new int[0];
        private readonly int[] m_SlotPersonIds = new int[BoneSlotLayout.m_SlotCount];
        private readonly int[] m_LockedPersonIds = new int[BoneSlotLayout.m_SlotCount];
        private readonly int[] m_SlotMissingFrameCounts = new int[BoneSlotLayout.m_SlotCount];
        private readonly int[] m_ExcludedPersonIds = new int[BoneSlotLayout.m_SlotCount];
        private readonly float[,] m_SlotRects = new float[BoneSlotLayout.m_SlotCount, m_RectElementCount];
        private int m_FrameSerialCounter;
        private int m_ConfiguredPlayerCount = BoneSlotLayout.m_SlotCount;
        private bool m_IsFrameStorageInitialized;
        private bool m_HasInitializedSlotLayout;
        private bool m_HasAppliedInitialPersonIds;

        public BoneFrameData ReadLatestFrameData()
        {
            return m_LatestFrameData;
        }

        public string ReadSourceName()
        {
            return "local_sdk";
        }

        public void Tick()
        {
        }

        public void Shutdown()
        {
        }

        public void ConfigureBattleSlotLayout(int playerCount, int[] initialPersonIds)
        {
            int normalizedPlayerCount = Mathf.Clamp(playerCount, 1, BoneSlotLayout.m_SlotCount);
            if (m_ConfiguredPlayerCount != normalizedPlayerCount || !m_HasInitializedSlotLayout)
            {
                m_ConfiguredPlayerCount = normalizedPlayerCount;
                m_HasInitializedSlotLayout = false;
                InitializeSlotLayout();
            }

            ApplyInitialPersonIds(initialPersonIds);
        }

        private void Awake()
        {
            InitializeSlotLayout();
        }

        private void Update()
        {
            if (!ReadIsSdkRuntimeReady())
            {
                MarkFrameUnavailable();
                return;
            }

            EnsureFrameStorageInitialized();
            UpdateGetPersonData();
        }

        public override void UpdateGetPersonData()
        {
            InitializeSlotLayout();
            long frameInfoPtr = GetLatestFrameInfoPtr();
            if (frameInfoPtr == 0)
            {
                MarkFrameUnavailable();
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

            PrepareFrame(frameTimeMs, imageWidth, imageHeight);
            UpdateSlotAssignments(frameInfoPtr, m_CurrentFramePersonIds, personCount);
            TraversePersion(frameInfoPtr, m_SlotPersonIds, m_SlotPersonIds.Length);
        }

        protected override void TraversePersion(long frameInfoPtr, int[] currentFramePersonIds, int perSonNumber)
        {
            for (int slotIndex = 0; slotIndex < perSonNumber; slotIndex++)
            {
                int personId = currentFramePersonIds[slotIndex];
                if (personId == YouDooSDKConstants.PersonIdNull)
                {
                    m_LatestFrameData.m_Persons.Add(CreateEmptyPersonData());
                    continue;
                }

                var personData = RentPersonData();
                personData.Reset();
                personData.m_PersonId = personId;

                if (!TryFillDetectPart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON, personId, personData.m_Body))
                {
                    RecyclePersonData(personData);
                    m_LatestFrameData.m_Persons.Add(CreateEmptyPersonData());
                    continue;
                }

                TryFillDetectPart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_LEFT_HAND, personId, personData.m_LeftHand);
                TryFillDetectPart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_RIGHT_HAND, personId, personData.m_RightHand);
                TryFillDetectPart(frameInfoPtr, YouDooSDKConstants.DetectType.DETECT_TYPE_FACE, personId, personData.m_Face);
                m_LatestFrameData.m_Persons.Add(personData);
            }
        }

        private bool TryFillDetectPart(
            long frameInfoPtr,
            YouDooSDKConstants.DetectType detectType,
            int objectId,
            BoneDetectPartData detectPart)
        {
            detectPart.Reset();
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
                return false;
            }

            detectPart.m_Rect.Set(left, top, right, bottom);
            detectPart.m_Score = score;
            detectPart.m_Type = type;

            int jointCount = Mathf.Min(keypointCount, detectPart.m_Joints.Length);
            bool hasTrackedJoint = false;
            for (int i = 0; i < jointCount; i++)
            {
                if (GetKeyPointByType(
                        frameInfoPtr,
                        (int)detectType,
                        objectId,
                        i,
                        out float x,
                        out float y,
                        out float z,
                        out float jointScore))
                {
                    detectPart.m_Joints[i].Set(x, y, z, jointScore);
                    hasTrackedJoint = true;
                }
            }

            return detectPart.m_Rect.m_IsValid || detectPart.m_Score > 0f || hasTrackedJoint;
        }

        private void PrepareFrame(long frameTimeMs, int imageWidth, int imageHeight)
        {
            RecycleFramePersons();
            m_LatestFrameData.Reset();
            m_LatestFrameData.m_HasFrameData = true;
            m_LatestFrameData.m_FrameSerial = ++m_FrameSerialCounter;
            m_LatestFrameData.m_FrameTimeMs = frameTimeMs;
            m_LatestFrameData.m_ImageWidth = imageWidth;
            m_LatestFrameData.m_ImageHeight = imageHeight;
        }

        private void MarkFrameUnavailable()
        {
            RecycleFramePersons();
            m_LatestFrameData.Reset();
            m_LatestFrameData.m_FrameSerial = ++m_FrameSerialCounter;
        }

        private void RecycleFramePersons()
        {
            for (int i = 0; i < m_LatestFrameData.m_Persons.Count; i++)
            {
                RecyclePersonData(m_LatestFrameData.m_Persons[i]);
            }
        }

        private BonePersonData RentPersonData()
        {
            return m_PersonPool.Count > 0 ? m_PersonPool.Dequeue() : new BonePersonData();
        }

        private void RecyclePersonData(BonePersonData personData)
        {
            if (personData == null)
            {
                return;
            }

            personData.Reset();
            m_PersonPool.Enqueue(personData);
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

        private void InitializeSlotLayout()
        {
            if (m_HasInitializedSlotLayout)
            {
                return;
            }

            ResetSlotAssignments();

            for (int slotIndex = 0; slotIndex < BoneSlotLayout.m_SlotCount; slotIndex++)
            {
                Rect sourceUvRect;
                if (TryReadPreparedSlotRect(slotIndex, out sourceUvRect) ||
                    BoneSlotLayout.TryReadPrepareUvRect(m_ConfiguredPlayerCount, slotIndex, out sourceUvRect))
                {
                    SetSlotRect(slotIndex, sourceUvRect.xMin, sourceUvRect.yMin, sourceUvRect.xMax, sourceUvRect.yMax);
                    continue;
                }

                SetInactiveSlotRect(slotIndex);
            }

            m_HasInitializedSlotLayout = true;
        }

        private void ResetSlotAssignments()
        {
            m_HasAppliedInitialPersonIds = false;
            for (int slotIndex = 0; slotIndex < BoneSlotLayout.m_SlotCount; slotIndex++)
            {
                m_SlotPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
                m_LockedPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
                m_SlotMissingFrameCounts[slotIndex] = 0;
            }
        }

        private void ApplyInitialPersonIds(int[] initialPersonIds)
        {
            if (m_HasAppliedInitialPersonIds || initialPersonIds == null)
            {
                return;
            }

            int count = Mathf.Min(m_ConfiguredPlayerCount, initialPersonIds.Length);
            for (int slotIndex = 0; slotIndex < count; slotIndex++)
            {
                int personId = initialPersonIds[slotIndex];
                if (personId == YouDooSDKConstants.PersonIdNull || ContainsInitialPersonBefore(slotIndex, personId))
                {
                    continue;
                }

                // 准备结果只用于战斗初始绑定。编号释放后不会再次强制写回，后续完全由战斗帧重新接管。
                m_LockedPersonIds[slotIndex] = personId;
                m_SlotMissingFrameCounts[slotIndex] = 0;
            }

            m_HasAppliedInitialPersonIds = true;
        }

        private bool ContainsInitialPersonBefore(int endSlotIndex, int personId)
        {
            for (int slotIndex = 0; slotIndex < endSlotIndex; slotIndex++)
            {
                if (m_LockedPersonIds[slotIndex] == personId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryReadPreparedSlotRect(int slotIndex, out Rect rect)
        {
            rect = Rect.zero;
            var parseDataDemo = global::AndroidParseDataDemo.Instance;
            var preparedRects = parseDataDemo != null ? parseDataDemo.personPlayerReadyRectf : null;
            if (preparedRects == null ||
                preparedRects.GetLength(0) != m_ConfiguredPlayerCount ||
                preparedRects.GetLength(0) <= slotIndex ||
                preparedRects.GetLength(1) < m_RectElementCount ||
                slotIndex >= m_ConfiguredPlayerCount)
            {
                return false;
            }

            // 真机准备界面会根据人数和 UI 显示槽位写入最终检测区，战斗阶段沿用这份结果。
            float left = preparedRects[slotIndex, 0];
            float top = preparedRects[slotIndex, 1];
            float right = preparedRects[slotIndex, 2];
            float bottom = preparedRects[slotIndex, 3];
            if (!IsValidSlotRect(left, top, right, bottom))
            {
                return false;
            }

            rect = new Rect(left, top, right - left, bottom - top);
            return true;
        }

        private void SetSlotRect(int slotIndex, float left, float top, float right, float bottom)
        {
            m_SlotRects[slotIndex, 0] = left;
            m_SlotRects[slotIndex, 1] = top;
            m_SlotRects[slotIndex, 2] = right;
            m_SlotRects[slotIndex, 3] = bottom;
        }

        private void SetInactiveSlotRect(int slotIndex)
        {
            SetSlotRect(slotIndex, 2f, 0f, 2f, 0f);
        }

        private static bool IsValidSlotRect(float left, float top, float right, float bottom)
        {
            return right - left >= 0.01f && bottom - top >= 0.01f;
        }

        private void UpdateSlotAssignments(long frameInfoPtr, int[] currentFramePersonIds, int personCount)
        {
            ClearFrameSlotAssignments();

            int excludeCount = 0;
            for (int slotIndex = 0; slotIndex < BoneSlotLayout.m_SlotCount; slotIndex++)
            {
                int personId = m_LockedPersonIds[slotIndex];
                if (personId == YouDooSDKConstants.PersonIdNull)
                {
                    continue;
                }

                if (TryResolveTrackedPerson(frameInfoPtr, currentFramePersonIds, personCount, slotIndex, personId))
                {
                    if (excludeCount < m_ExcludedPersonIds.Length)
                    {
                        m_ExcludedPersonIds[excludeCount++] = m_SlotPersonIds[slotIndex];
                    }

                    continue;
                }

                m_SlotMissingFrameCounts[slotIndex]++;
                if (m_SlotMissingFrameCounts[slotIndex] > m_MissingFrameThreshold)
                {
                    // 只释放当前控制权，不记录离开名单；原人物返回后仍可和其他人物一样参与空槽位绑定。
                    m_LockedPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
                    m_SlotMissingFrameCounts[slotIndex] = 0;
                }
            }

            for (int slotIndex = 0; slotIndex < BoneSlotLayout.m_SlotCount; slotIndex++)
            {
                if (m_LockedPersonIds[slotIndex] != YouDooSDKConstants.PersonIdNull)
                {
                    continue;
                }

                if (!TryFindReplacementPerson(frameInfoPtr, slotIndex, excludeCount, out int foundPersonId))
                {
                    continue;
                }

                m_SlotPersonIds[slotIndex] = foundPersonId;
                m_LockedPersonIds[slotIndex] = foundPersonId;
                m_SlotMissingFrameCounts[slotIndex] = 0;
                if (excludeCount < m_ExcludedPersonIds.Length)
                {
                    m_ExcludedPersonIds[excludeCount++] = foundPersonId;
                }
            }
        }

        private void ClearFrameSlotAssignments()
        {
            for (int slotIndex = 0; slotIndex < BoneSlotLayout.m_SlotCount; slotIndex++)
            {
                m_SlotPersonIds[slotIndex] = YouDooSDKConstants.PersonIdNull;
            }
        }

        private bool TryResolveTrackedPerson(long frameInfoPtr, int[] currentFramePersonIds, int personCount, int slotIndex, int personId)
        {
            // 战斗内一旦已经锁定到某个人，就不再用区域把他踢掉。
            // 当前帧只要还能识别到这个骨骼并且置信度有效，就继续沿用。
            if (ReadContainsPersonId(currentFramePersonIds, personCount, personId) &&
                TryReadPersonScore(frameInfoPtr, personId, out float score) &&
                score >= m_MinTrackedScore)
            {
                m_SlotPersonIds[slotIndex] = personId;
                m_SlotMissingFrameCounts[slotIndex] = 0;
                return true;
            }

            // 短暂漏检期间保持原骨骼编号，不立即换绑旁边的人。
            // 只有外层累计到彻底丢失阈值并释放编号后，下一轮才按当前画面重新接管。
            return false;
        }

        private bool TryFindReplacementPerson(long frameInfoPtr, int slotIndex, int excludeCount, out int foundPersonId)
        {
            if (m_ConfiguredPlayerCount <= 1)
            {
                foundPersonId = CheckPersonScoreInArea(
                    0,
                    0f,
                    0f,
                    1f,
                    1f,
                    m_ExcludedPersonIds,
                    excludeCount);
            }
            else
            {
                foundPersonId = CheckPersonScoreInArea(
                    0,
                    m_SlotRects[slotIndex, 0],
                    m_SlotRects[slotIndex, 1],
                    m_SlotRects[slotIndex, 2],
                    m_SlotRects[slotIndex, 3],
                    m_ExcludedPersonIds,
                    excludeCount);
            }

            return foundPersonId != YouDooSDKConstants.PersonIdNull &&
                TryReadPersonScore(frameInfoPtr, foundPersonId, out float score) &&
                score >= m_MinTrackedScore;
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

        private BonePersonData CreateEmptyPersonData()
        {
            var personData = RentPersonData();
            personData.Reset();
            return personData;
        }

        private static bool ReadIsSdkRuntimeReady()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return true;
            }

            var serverInfo = GameObject.FindFirstObjectByType<AndroidServerInfo>();
            return serverInfo != null && serverInfo.IsSDKMode;
        }
    }
}
