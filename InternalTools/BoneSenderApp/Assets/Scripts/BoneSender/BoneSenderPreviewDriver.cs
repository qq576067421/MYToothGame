using UnityEngine;

namespace BoneSender
{
    /// <summary>
    /// 将 BoneSenderApp 当前采集到的骨骼帧，按 SDK demo 的显示链路喂给 PlayerMatchView 与 PlayerTextuerShow。
    /// 这个类只负责本地预览，不参与网络发送协议。
    /// </summary>
    [DefaultExecutionOrder(-31950)]
    public sealed class BoneSenderPreviewDriver : MonoBehaviour
    {
        private const int m_PreviewSlotCount = 4;
        private const int m_RectElementCount = 4;
        private const int m_KeyPointElementCount = 4;

        public SenderBoneParseData m_ParseData;

        private readonly float[,] m_PreviewReadyRects = new float[m_PreviewSlotCount, m_RectElementCount];
        private PlayerTextuerShow m_PlayerTextuerShow;
        private bool m_HasInitializedPreview;
        private bool m_HasLoggedMissingParseData;
        private bool m_HasLoggedMissingPlayerTextureShow;
        private bool m_HasLoggedWaitingForPlayerTextureBinding;
        private bool m_HasLoggedPreviewReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachAfterSceneLoad()
        {
            SenderBoneParseData[] parseDataList =
                GameObject.FindObjectsByType<SenderBoneParseData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < parseDataList.Length; i++)
            {
                SenderBoneParseData parseData = parseDataList[i];
                if (parseData == null || parseData.GetComponent<BoneSenderPreviewDriver>() != null)
                {
                    continue;
                }

                BoneSenderPreviewDriver previewDriver = parseData.gameObject.AddComponent<BoneSenderPreviewDriver>();
                previewDriver.m_ParseData = parseData;
            }
        }

        private void Awake()
        {
            TryResolveParseData();
        }

        private void Update()
        {
            if (!TryEnsurePreviewReady())
            {
                return;
            }

            RefreshPreview(m_ParseData != null ? m_ParseData.ReadLatestFrame() : null);
        }

        private bool TryEnsurePreviewReady()
        {
            TryResolveParseData();
            if (m_ParseData == null)
            {
                if (!m_HasLoggedMissingParseData)
                {
                    m_HasLoggedMissingParseData = true;
                    BoneSenderAppLogger.LogWarning("骨骼预览驱动未找到 SenderBoneParseData，暂不刷新摄像头图像与骨骼显示");
                }

                return false;
            }

            m_HasLoggedMissingParseData = false;

            TryResolvePlayerTextureShow();
            if (m_PlayerTextuerShow == null)
            {
                if (!m_HasLoggedMissingPlayerTextureShow)
                {
                    m_HasLoggedMissingPlayerTextureShow = true;
                    BoneSenderAppLogger.LogWarning("骨骼预览驱动未找到 PlayerTextuerShow，暂不刷新摄像头图像与骨骼显示");
                }

                return false;
            }

            m_HasLoggedMissingPlayerTextureShow = false;
            if (!IsPlayerTextureShowReadyForInit())
            {
                if (!m_HasLoggedWaitingForPlayerTextureBinding)
                {
                    m_HasLoggedWaitingForPlayerTextureBinding = true;
                    BoneSenderAppLogger.Log("骨骼预览驱动等待 PlayerTextuerShow 自动补绑完成后再启动预览");
                }

                return false;
            }

            m_HasLoggedWaitingForPlayerTextureBinding = false;
            if (m_HasInitializedPreview)
            {
                return true;
            }

            PlayerMatchView.Instance.InitPlayerMatchView(m_PreviewSlotCount, PlayerMatchViewMode.PartitionView);
            PlayerMatchView.Instance.SetpersonPlayerReadyPartitionRectf(m_PreviewSlotCount, m_PreviewReadyRects);
            m_PlayerTextuerShow.InitGameInfo(m_PreviewReadyRects, m_PreviewSlotCount);
            m_HasInitializedPreview = true;

            if (!m_HasLoggedPreviewReady)
            {
                m_HasLoggedPreviewReady = true;
                BoneSenderAppLogger.Log("骨骼预览驱动已启动，当前按 PartitionView 水平4槽模式刷新摄像头图像与骨骼");
            }

            return true;
        }

        private void RefreshPreview(BoneProtocolFrame frame)
        {
            PlayerMatchView.Instance.ResetData();
            m_PlayerTextuerShow.SetSkeletsonHide();

            for (int slotIndex = 0; slotIndex < m_PreviewSlotCount; slotIndex++)
            {
                BoneProtocolPerson person = ReadPerson(frame, slotIndex);
                if (!TryBuildBodyKeyPointArray(person != null ? person.m_Body : null, out float[,] poseKeyPoints))
                {
                    PlayerMatchView.Instance.SetPersonPlayerReadyDrawPartitionRectf(null, slotIndex);
                    continue;
                }

                float score = person != null && person.m_Body != null ? person.m_Body.m_Score : 0f;
                PlayerMatchView.Instance.SetPersonPlayerRectf(poseKeyPoints, person.m_PersonId, score);
                PlayerMatchView.Instance.SetPersonPlayerReadyDrawPartitionRectf(poseKeyPoints, slotIndex);

                int skeletonIndex = ReadSkeletonIndexForSlot(slotIndex);
                if (skeletonIndex < 0)
                {
                    continue;
                }

                BoneProtocolRect bodyRect = person != null && person.m_Body != null ? person.m_Body.m_Rect : null;
                m_PlayerTextuerShow.DrawSkeleton(
                    skeletonIndex,
                    poseKeyPoints,
                    null,
                    null,
                    bodyRect != null ? bodyRect.m_Left : 0f,
                    bodyRect != null ? bodyRect.m_Top : 0f,
                    bodyRect != null ? bodyRect.m_Right : 0f,
                    bodyRect != null ? bodyRect.m_Bottom : 0f,
                    person.m_PersonId,
                    score);
            }

            if (PlayerMatchView.Instance.CheckCameraTextureViewManager() && m_PlayerTextuerShow.gameObject.activeSelf)
            {
                m_PlayerTextuerShow.SetCameraTextureViewBgRect(PlayerMatchView.Instance.CalculationResult());
            }

            if (m_PlayerTextuerShow.gameObject.activeSelf)
            {
                m_PlayerTextuerShow.ShowCameraImage();
            }
        }

        private void TryResolveParseData()
        {
            if (m_ParseData == null)
            {
                m_ParseData = GetComponent<SenderBoneParseData>();
            }

            if (m_ParseData == null)
            {
                m_ParseData = GameObject.FindFirstObjectByType<SenderBoneParseData>();
            }
        }

        private void TryResolvePlayerTextureShow()
        {
            if (m_PlayerTextuerShow != null)
            {
                return;
            }

            PlayerTextuerShow[] allShows = Resources.FindObjectsOfTypeAll<PlayerTextuerShow>();
            for (int i = 0; i < allShows.Length; i++)
            {
                PlayerTextuerShow targetShow = allShows[i];
                if (targetShow == null || !targetShow.gameObject.scene.IsValid())
                {
                    continue;
                }

                m_PlayerTextuerShow = targetShow;
                return;
            }
        }

        private int ReadSkeletonIndexForSlot(int slotIndex)
        {
            if (m_PlayerTextuerShow == null ||
                m_PlayerTextuerShow.Playerskeletons == null ||
                m_PlayerTextuerShow.Playerskeletons.Length <= 0)
            {
                return -1;
            }

            int offset = Mathf.Min(m_PreviewSlotCount, m_PlayerTextuerShow.Playerskeletons.Length);
            int skeletonIndex = m_PlayerTextuerShow.Playerskeletons.Length - offset + slotIndex;
            return skeletonIndex >= 0 && skeletonIndex < m_PlayerTextuerShow.Playerskeletons.Length
                ? skeletonIndex
                : -1;
        }

        private static BoneProtocolPerson ReadPerson(BoneProtocolFrame frame, int slotIndex)
        {
            if (frame == null || frame.m_Persons == null || slotIndex < 0 || slotIndex >= frame.m_Persons.Length)
            {
                return null;
            }

            BoneProtocolPerson person = frame.m_Persons[slotIndex];
            return person != null && person.m_PersonId != YouDooSDKConstants.PersonIdNull ? person : null;
        }

        private static bool TryBuildBodyKeyPointArray(BoneProtocolPart bodyPart, out float[,] keyPoints)
        {
            keyPoints = null;
            if (bodyPart == null ||
                bodyPart.m_Joints == null ||
                bodyPart.m_Joints.Length < (int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT)
            {
                return false;
            }

            if (!HasTrackedBodyJoint(bodyPart, (int)YouDooSDKConstants.KeyPointIndex.Nose) ||
                !HasTrackedBodyJoint(bodyPart, (int)YouDooSDKConstants.KeyPointIndex.Leftshoulder) ||
                !HasTrackedBodyJoint(bodyPart, (int)YouDooSDKConstants.KeyPointIndex.Rightshoulder) ||
                !HasTrackedBodyJoint(bodyPart, (int)YouDooSDKConstants.KeyPointIndex.Leftankle) ||
                !HasTrackedBodyJoint(bodyPart, (int)YouDooSDKConstants.KeyPointIndex.Rightankle))
            {
                return false;
            }

            int jointCount = (int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT;
            keyPoints = new float[jointCount, m_KeyPointElementCount];
            for (int jointIndex = 0; jointIndex < jointCount; jointIndex++)
            {
                BoneProtocolJoint joint = bodyPart.m_Joints[jointIndex];
                if (joint == null || !joint.m_IsTracked)
                {
                    continue;
                }

                keyPoints[jointIndex, 0] = joint.m_X;
                keyPoints[jointIndex, 1] = joint.m_Y;
                keyPoints[jointIndex, 2] = joint.m_Z;
                keyPoints[jointIndex, 3] = joint.m_Score;
            }

            return true;
        }

        private static bool HasTrackedBodyJoint(BoneProtocolPart bodyPart, int jointIndex)
        {
            return bodyPart != null &&
                   bodyPart.m_Joints != null &&
                   jointIndex >= 0 &&
                   jointIndex < bodyPart.m_Joints.Length &&
                   bodyPart.m_Joints[jointIndex] != null &&
                   bodyPart.m_Joints[jointIndex].m_IsTracked;
        }

        private bool IsPlayerTextureShowReadyForInit()
        {
            return m_PlayerTextuerShow != null &&
                   m_PlayerTextuerShow.rawPointPrefab != null &&
                   m_PlayerTextuerShow.rawLinePrefab != null &&
                   m_PlayerTextuerShow.Playerskeletons != null &&
                   m_PlayerTextuerShow.Playerskeletons.Length > 0;
        }
    }
}
