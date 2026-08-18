using BoneSender.TestInput;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BoneSender
{
    public class BoneSenderInfoPresenter : MonoBehaviour
    {
        private static readonly string[] m_SlotRangeTexts =
        {
            "X: 0.100 - 0.375",
            "X: 0.375 - 0.500",
            "X: 0.500 - 0.625",
            "X: 0.625 - 1.000",
        };

        public BoneSenderRuntime m_Runtime;

        public List<Text> m_Infos = new List<Text>();
        public Text m_CommonInfo;

        private readonly StringBuilder m_CommonInfoBuilder = new StringBuilder(512);
        private readonly StringBuilder m_TargetHostsBuilder = new StringBuilder(256);
        private readonly StringBuilder m_SlotInfoBuilder = new StringBuilder(1536);
        private readonly BoneSenderInputTestEvaluator m_TestInputEvaluator = new BoneSenderInputTestEvaluator();
        private readonly List<BoneSenderTestInputSeatState> m_SlotTestInputStates = new List<BoneSenderTestInputSeatState>(4);

        private void Awake()
        {
            TryResolveInfoTexts();
        }

        private void Update()
        {
            TryResolveInfoTexts();
            if (m_CommonInfo == null && (m_Infos == null || m_Infos.Count <= 0))
            {
                return;
            }

            if (m_Runtime == null)
            {
                SetCommonInfo("骨骼发送运行对象缺失");
                ClearSlotInfos();
                return;
            }

            SetCommonInfo(BuildCommonInfoText());
            RefreshSlotInfos();
        }

        private void TryResolveInfoTexts()
        {
            if (m_CommonInfo == null)
            {
                m_CommonInfo = FindTextByName("info");
            }

            if (m_Infos == null)
            {
                m_Infos = new List<Text>();
            }

            if (m_Infos.Count <= 0)
            {
                TryAppendSlotText("info1");
                TryAppendSlotText("info2");
                TryAppendSlotText("info3");
                TryAppendSlotText("info4");
            }
        }

        private void ClearSlotInfos()
        {
            if (m_Infos == null)
            {
                return;
            }

            ResetTestInputStates();

            for (int i = 0; i < m_Infos.Count; i++)
            {
                SetInfo(i, string.Empty);
            }
        }

        private string BuildCommonInfoText()
        {
            m_CommonInfoBuilder.Length = 0;
            AppendLine(m_CommonInfoBuilder, "输入来源", m_Runtime.ReadInputSourceDisplayName());
            AppendLine(m_CommonInfoBuilder, "显示布局", m_Runtime.ReadPreviewLayoutDisplayName());
            AppendLine(m_CommonInfoBuilder, "槽位绑定", m_Runtime.ReadUiSlotBindingSummary());
            AppendLine(m_CommonInfoBuilder, "网络状态", m_Runtime.ReadNetworkStateDisplayName());
            AppendLine(m_CommonInfoBuilder, "当前目标", m_Runtime.ReadCurrentTargetHost() + ":" + m_Runtime.ReadTargetPort());
            AppendLine(m_CommonInfoBuilder, "候选目标", BuildTargetHostsText());
            AppendLine(m_CommonInfoBuilder, "目标发送频率", m_Runtime.ReadConfiguredSendFramesPerSecond() + " 帧/秒");
            AppendLine(m_CommonInfoBuilder, "实际发送频率", m_Runtime.ReadMeasuredSendFramesPerSecond().ToString("F1") + " 帧/秒");
            AppendLine(
                m_CommonInfoBuilder,
                "心跳状态",
                string.Format(
                    "{0:F0} 秒/次，下次约 {1:F1} 秒",
                    m_Runtime.ReadHeartbeatIntervalSeconds(),
                    m_Runtime.ReadSecondsUntilNextHeartbeat()));
            AppendLine(m_CommonInfoBuilder, "最近骨骼数据", BuildLatestDataSummary());
            AppendLine(m_CommonInfoBuilder, "最近成功发送", BuildLatestSendSummary());
            return m_CommonInfoBuilder.ToString();
        }

        private void RefreshSlotInfos()
        {
            if (m_Infos == null)
            {
                return;
            }

            BoneProtocolFrame frame = m_Runtime.ReadLastSentFrameSnapshot();
            EnsureTestInputStateCapacity(m_Infos.Count);
            for (int slotIndex = 0; slotIndex < m_Infos.Count; slotIndex++)
            {
                BoneProtocolPerson person = frame != null && frame.m_Persons != null && slotIndex < frame.m_Persons.Length
                    ? frame.m_Persons[slotIndex]
                    : null;
                SetInfo(
                    slotIndex,
                    BuildSlotInfoText(
                        slotIndex,
                        person,
                        m_SlotTestInputStates[slotIndex],
                        frame != null ? frame.m_CaptureTimeMs : 0L));
            }
        }

        public void SetCommonInfo(string infoText)
        {
            if (m_CommonInfo == null)
            {
                return;
            }

            m_CommonInfo.text = infoText ?? string.Empty;
        }

        public void SetInfo(int index, string infoText)
        {
            if (m_Infos == null || index < 0 || index >= m_Infos.Count)
            {
                return;
            }

            Text targetText = m_Infos[index];
            if (targetText == null)
            {
                return;
            }

            targetText.text = infoText ?? string.Empty;
        }

        public void ClearInfos()
        {
            SetCommonInfo(string.Empty);
            ClearSlotInfos();
        }

        private string BuildLatestDataSummary()
        {
            int personCount = m_Runtime.ReadLastSentPersonCount();
            long dataAgeMs = m_Runtime.ReadLastSentDataAgeMilliseconds();
            string dataAgeText = dataAgeMs >= 0L ? dataAgeMs + " 毫秒前" : "暂无数据";
            return string.Format(
                "{0}，有效人数 {1} 人，采集时间 {2}",
                m_Runtime.ReadLastSentDataTypeDisplayName(),
                personCount,
                dataAgeText);
        }

        private string BuildLatestSendSummary()
        {
            int frameSerial = m_Runtime.ReadLastSentFrameSerial();
            long sendAgeMs = m_Runtime.ReadLastSendAgeMilliseconds();
            string sendAgeText = sendAgeMs >= 0L ? sendAgeMs + " 毫秒前" : "尚未发送";
            return string.Format("第 {0} 帧，发送时间 {1}", frameSerial, sendAgeText);
        }

        private string BuildSlotInfoText(
            int slotIndex,
            BoneProtocolPerson person,
            BoneSenderTestInputSeatState testInputState,
            long frameTimeMs)
        {
            m_SlotInfoBuilder.Length = 0;
            AppendLine(m_SlotInfoBuilder, "槽位", (slotIndex + 1) + "（" + ReadSlotRangeText(slotIndex) + "）");
            AppendLine(m_SlotInfoBuilder, "绑定SDK槽位", BuildBoundSdkSlotText(slotIndex));
            if (!IsTrackedPerson(person))
            {
                if (testInputState != null)
                {
                    testInputState.ResetForNoPerson();
                }

                AppendLine(m_SlotInfoBuilder, "状态", "空位");
                AppendLine(m_SlotInfoBuilder, "骨骼数据", "当前没有发送该槽位的人体骨骼数据");
                return m_SlotInfoBuilder.ToString();
            }

            AppendLine(m_SlotInfoBuilder, "状态", "已跟踪");
            AppendLine(m_SlotInfoBuilder, "人物标识", person.m_PersonId.ToString());
            AppendLine(m_SlotInfoBuilder, "身体框", ReadRectText(person.m_Body));
            AppendLine(m_SlotInfoBuilder, "身体点数", ReadTrackedJointCountText(person.m_Body));
            AppendLine(m_SlotInfoBuilder, "左手点数", ReadTrackedJointCountText(person.m_LeftHand));
            AppendLine(m_SlotInfoBuilder, "右手点数", ReadTrackedJointCountText(person.m_RightHand));
            AppendLine(m_SlotInfoBuilder, "人脸点数", ReadTrackedJointCountText(person.m_Face));
            AppendLine(m_SlotInfoBuilder, "左手SDK位置", ReadBodyJointText(person.m_Body, (int)YouDooSDKConstants.KeyPointIndex.Leftwrist));
            AppendLine(m_SlotInfoBuilder, "右手SDK位置", ReadBodyJointText(person.m_Body, (int)YouDooSDKConstants.KeyPointIndex.Rightwrist));
            AppendTestInputLines(person, testInputState, frameTimeMs);
            return m_SlotInfoBuilder.ToString();
        }

        private void AppendTestInputLines(
            BoneProtocolPerson person,
            BoneSenderTestInputSeatState testInputState,
            long frameTimeMs)
        {
            BoneSenderTestInputResult testInputResult = m_TestInputEvaluator.Evaluate(person, testInputState, frameTimeMs);
            AppendLine(m_SlotInfoBuilder, "朝向信息", ReadTestTurnSummaryText(testInputResult));
            AppendLine(m_SlotInfoBuilder, "左肩SDK位置", ReadTurnJointText(testInputResult.m_IsTurnAvailable, testInputResult.m_LeftShoulderX, testInputResult.m_LeftShoulderY));
            AppendLine(m_SlotInfoBuilder, "右肩SDK位置", ReadTurnJointText(testInputResult.m_IsTurnAvailable, testInputResult.m_RightShoulderX, testInputResult.m_RightShoulderY));
            AppendLine(m_SlotInfoBuilder, "带方向肩高差", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_SignedShoulderDeltaNormalized));
            AppendLine(m_SlotInfoBuilder, "肩宽归一化", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_AbsShoulderDeltaNormalized));
            AppendLine(m_SlotInfoBuilder, "肩宽变化", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_ShoulderDeltaChangeNormalized));
            AppendLine(m_SlotInfoBuilder, "肩高差", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_ShoulderYGap));
            AppendLine(m_SlotInfoBuilder, "肩中心位移", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_ShoulderCenterDelta));
            AppendLine(m_SlotInfoBuilder, "胯中心位移", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_HipCenterDelta));
            AppendLine(m_SlotInfoBuilder, "历史最大肩宽", ReadTurnMetricText(testInputResult.m_IsTurnAvailable, testInputResult.m_MaxObservedShoulderDeltaNormalized));
        }

        private static void AppendLine(StringBuilder stringBuilder, string title, string value)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append('\n');
            }

            stringBuilder.Append(title).Append("：").Append(value);
        }

        private static bool IsTrackedPerson(BoneProtocolPerson person)
        {
            return person != null && person.m_PersonId != YouDooSDKConstants.PersonIdNull;
        }

        private void EnsureTestInputStateCapacity(int slotCount)
        {
            while (m_SlotTestInputStates.Count < slotCount)
            {
                m_SlotTestInputStates.Add(new BoneSenderTestInputSeatState());
            }
        }

        private void ResetTestInputStates()
        {
            for (int i = 0; i < m_SlotTestInputStates.Count; i++)
            {
                BoneSenderTestInputSeatState testInputState = m_SlotTestInputStates[i];
                if (testInputState != null)
                {
                    testInputState.ResetForNoPerson();
                }
            }
        }

        private static string ReadTestTurnSummaryText(BoneSenderTestInputResult testInputResult)
        {
            if (!testInputResult.m_IsTurnAvailable)
            {
                return string.Format(
                    "暂时无法判断，当前维持{0} {1:F1} 度，原因={2}",
                    ReadTurnStateText(testInputResult.m_TurnState),
                    testInputResult.m_TurnAngleDegrees,
                    string.IsNullOrEmpty(testInputResult.m_TurnUnavailableReason) ? "缺少关键点" : testInputResult.m_TurnUnavailableReason);
            }

            return string.Format(
                "{0}，{1:F1} 度，角速度 {2:F1} 度/秒，强度 {3:F2}",
                ReadTurnStateText(testInputResult.m_TurnState),
                testInputResult.m_TurnAngleDegrees,
                testInputResult.m_TurnSpeed,
                testInputResult.m_TurnStrength);
        }

        private static string ReadTurnJointText(bool isTurnAvailable, float x, float y)
        {
            return isTurnAvailable
                ? string.Format("({0:F3}, {1:F3})", x, y)
                : "缺失";
        }

        private static string ReadTurnMetricText(bool isTurnAvailable, float value)
        {
            return isTurnAvailable ? value.ToString("F3") : "缺失";
        }

        private static string ReadTurnStateText(BoneSenderTestTurnState turnState)
        {
            switch (turnState)
            {
                case BoneSenderTestTurnState.TurningRight:
                    return "向右转中";
                case BoneSenderTestTurnState.StableRight:
                    return "右侧停稳";
                case BoneSenderTestTurnState.ReturningFromRight:
                    return "从右回正中";
                case BoneSenderTestTurnState.TurningLeft:
                    return "向左转中";
                case BoneSenderTestTurnState.StableLeft:
                    return "左侧停稳";
                case BoneSenderTestTurnState.ReturningFromLeft:
                    return "从左回正中";
                case BoneSenderTestTurnState.Neutral:
                default:
                    return "中立";
            }
        }

        private static string ReadSlotRangeText(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < m_SlotRangeTexts.Length
                ? m_SlotRangeTexts[slotIndex]
                : "未定义";
        }

        private string BuildBoundSdkSlotText(int uiSlotIndex)
        {
            int sdkSlotDisplayIndex = m_Runtime != null ? m_Runtime.ReadBoundSdkSlotDisplayIndex(uiSlotIndex) : 0;
            if (sdkSlotDisplayIndex <= 0)
            {
                return "未配置";
            }

            int sdkSlotIndex = sdkSlotDisplayIndex - 1;
            return sdkSlotDisplayIndex + "（" + ReadSlotRangeText(sdkSlotIndex) + "）";
        }

        private static string ReadTrackedJointCountText(BoneProtocolPart part)
        {
            if (part == null || part.m_Joints == null)
            {
                return "0/0";
            }

            int trackedJointCount = 0;
            for (int i = 0; i < part.m_Joints.Length; i++)
            {
                BoneProtocolJoint joint = part.m_Joints[i];
                if (joint != null && joint.m_IsTracked)
                {
                    trackedJointCount++;
                }
            }

            return trackedJointCount + "/" + part.m_Joints.Length;
        }

        private static string ReadRectText(BoneProtocolPart part)
        {
            if (part == null || part.m_Rect == null || !part.m_Rect.m_IsTracked)
            {
                return "未跟踪";
            }

            return string.Format(
                "({0:F3}, {1:F3}) - ({2:F3}, {3:F3})",
                part.m_Rect.m_Left,
                part.m_Rect.m_Top,
                part.m_Rect.m_Right,
                part.m_Rect.m_Bottom);
        }

        private static string ReadBodyJointText(BoneProtocolPart part, int jointIndex)
        {
            if (part == null || part.m_Joints == null || jointIndex < 0 || jointIndex >= part.m_Joints.Length)
            {
                return "缺失";
            }

            BoneProtocolJoint joint = part.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked)
            {
                return "未跟踪";
            }

            return string.Format("({0:F3}, {1:F3})", joint.m_X, joint.m_Y);
        }

        private string BuildTargetHostsText()
        {
            string[] targetHosts = m_Runtime.ReadTargetHosts();
            if (targetHosts == null || targetHosts.Length <= 0)
            {
                return "未配置";
            }

            m_TargetHostsBuilder.Length = 0;
            for (int i = 0; i < targetHosts.Length; i++)
            {
                if (i > 0)
                {
                    m_TargetHostsBuilder.Append(" | ");
                }

                m_TargetHostsBuilder.Append(targetHosts[i]);
            }

            return m_TargetHostsBuilder.ToString();
        }

        private void TryAppendSlotText(string objectName)
        {
            Text targetText = FindTextByName(objectName);
            if (targetText != null)
            {
                m_Infos.Add(targetText);
            }
        }

        private static Text FindTextByName(string objectName)
        {
            Text[] textComponents = GameObject.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < textComponents.Length; i++)
            {
                Text textComponent = textComponents[i];
                if (textComponent != null && textComponent.name == objectName)
                {
                    return textComponent;
                }
            }

            return null;
        }
    }
}
