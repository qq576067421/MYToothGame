using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow;
using YoudooGame.MatchFlow.Runtime;

namespace GameDll
{
    public sealed class BattleBoneParseData : MonoBehaviour, IBoneFrameSource
    {
        private const float m_TrackingFrameTimeoutSeconds = 2f;
        private readonly BoneFrameData m_LatestFrameData = new BoneFrameData();
        private readonly Queue<BonePersonData> m_PersonPool = new Queue<BonePersonData>();
        private int m_ConfiguredPlayerCount = BoneSlotLayout.m_SlotCount;
        private int m_FrameSerial;
        private MatchTrackingFrameSnapshot m_LastTrackingFrame;
        private float m_LastTrackingFrameTime;
        private float m_NextDiagnosticTime;
        private string m_LastDiagnosticState;
        private BattleStartupRequest m_BattleRequest;

        public BoneFrameData ReadLatestFrameData()
        {
            return m_LatestFrameData;
        }

        public bool TryReadLockedPersonId(int slotIndex, out int personId)
        {
            personId = BoneTrackedLayout.m_InvalidPersonId;
            if (slotIndex < 0 || slotIndex >= m_ConfiguredPlayerCount ||
                !YoudooAPI.TryGetSessionSnapshot(out MatchSessionSnapshot snapshot) ||
                !IsBattleSnapshot(snapshot) || !snapshot.TryGetBoundPlayer(slotIndex, out TrackedPlayer player) ||
                player == null)
            {
                return false;
            }

            personId = player.PersonId;
            return personId != BoneTrackedLayout.m_InvalidPersonId;
        }

        public string ReadSourceName()
        {
            return "framework_session";
        }

        public void Tick()
        {
        }

        public void Shutdown()
        {
            enabled = false;
            MarkFrameUnavailable();
        }

        public void ConfigureBattleSlotLayout(int playerCount, int[] initialPersonIds, BattleStartupRequest request = null)
        {
            m_BattleRequest = request;
            m_ConfiguredPlayerCount = Mathf.Clamp(playerCount, 1, BoneSlotLayout.m_SlotCount);
            MarkFrameUnavailable();
        }

        private void OnEnable()
        {
            enabled = true;
        }

        private bool IsBattleSnapshot(MatchSessionSnapshot snapshot)
        {
            return snapshot != null && (m_BattleRequest == null ||
                (m_BattleRequest.m_FrameworkSessionToken == snapshot.SessionToken &&
                 m_BattleRequest.m_FrameworkRoundId == snapshot.RoundSnapshot?.RoundId));
        }

        private void Update()
        {
            YoudooAPI.TryGetSessionSnapshot(out MatchSessionSnapshot snapshot);
            var trackingFrame = snapshot?.CurrentTrackingFrame;
            if (!IsBattleSnapshot(snapshot) ||
                snapshot.Phase != GameSessionPhase.Playing ||
                snapshot.LifecycleState != MatchSessionLifecycleState.Running ||
                trackingFrame == null)
            {
                MarkFrameUnavailable();
                LogTrackingDiagnostic(snapshot, trackingFrame, false);
                return;
            }

            // SDK 的会话序号在来源重连后仍会推进，原生帧号可能重用。
            if (m_LastTrackingFrame != null && trackingFrame.SessionToken == m_LastTrackingFrame.SessionToken &&
                trackingFrame.Sequence == m_LastTrackingFrame.Sequence)
            {
                bool expired = Time.unscaledTime - m_LastTrackingFrameTime >= m_TrackingFrameTimeoutSeconds;
                if (expired && m_LatestFrameData.m_HasFrameData)
                {
                    ClearFrameData();
                }

                LogTrackingDiagnostic(snapshot, trackingFrame, expired);
                return;
            }

            m_LastTrackingFrame = trackingFrame;
            m_LastTrackingFrameTime = Time.unscaledTime;
            RecycleFramePersons();
            m_LatestFrameData.Reset();
            m_LatestFrameData.m_HasFrameData = true;
            m_LatestFrameData.m_HasAuthoritativeSlotBindings = true;
            m_LatestFrameData.m_FrameSerial = ++m_FrameSerial;
            m_LatestFrameData.m_FrameTimeMs = (long)(trackingFrame.TimestampSec * 1000d);

            for (int slotIndex = 0; slotIndex < m_ConfiguredPlayerCount; slotIndex++)
            {
                if (snapshot.CanOperate(slotIndex) &&
                    snapshot.TryGetSlot(slotIndex, out MatchSessionSlotSnapshot slot) &&
                    snapshot.TryGetOperablePlayer(slotIndex, out TrackedPlayer player) &&
                    player != null && player.PersonId == slot.BoundPersonId)
                {
                    var person = RentPersonData();
                    FillPerson(player, person);
                    m_LatestFrameData.m_Persons.Add(person);
                }
                else
                {
                    m_LatestFrameData.m_Persons.Add(CreateEmptyPersonData());
                }
            }

            LogTrackingDiagnostic(snapshot, trackingFrame, false);
        }

        private void LogTrackingDiagnostic(MatchSessionSnapshot snapshot, MatchTrackingFrameSnapshot trackingFrame, bool expired)
        {
            float now = Time.unscaledTime;
            if (now < m_NextDiagnosticTime)
            {
                return;
            }

            m_NextDiagnosticTime = now + 1f;
            var state = new StringBuilder();
            state.Append(snapshot?.LifecycleState).Append('/').Append(snapshot?.Phase)
                .Append('/').Append(snapshot?.FrameState).Append(" endMode=").Append(snapshot?.GameplayEndMode)
                .Append(" hold=").Append(snapshot?.TrackingHoldReason);
            bool needsRecovery = false;
            if (snapshot != null)
            {
                foreach (var slot in snapshot.Slots)
                {
                    bool operable = snapshot.CanOperate(slot.SlotIndex);
                    state.Append(" slot=").Append(slot.SlotIndex).Append('/').Append(slot.Phase)
                        .Append('/').Append(slot.BindingState).Append(" bound=").Append(slot.BoundPersonId)
                        .Append(" candidate=").Append(slot.CandidatePersonId).Append(" op=").Append(operable ? 1 : 0);
                    needsRecovery |= !operable;
                }
            }

            if (expired)
            {
                state.Append(" frameExpired=1");
            }

            string status = state.ToString();
            bool changed = status != m_LastDiagnosticState;
            m_LastDiagnosticState = status;
            if (!changed && (snapshot?.Phase != GameSessionPhase.Playing || !needsRecovery && !expired))
            {
                return;
            }

            var provider = PlatformServices.Get<IPlayerTrackingProvider>();
            var providerFrameResult = provider?.GetLatestPlayerFrame();
            var providerFrame = providerFrameResult.HasValue && providerFrameResult.Value.Success
                ? providerFrameResult.Value.Value
                : null;
            var target = PlatformServices.Get<ITrackingTargetProvider>()?.CurrentTrackingTarget;
            state.Append(" time=").Append(now.ToString("F1"))
                .Append(" session=").Append(snapshot?.SessionToken)
                .Append(" sdkSeq=").Append(trackingFrame?.Sequence)
                .Append(" providerFrame=").Append(providerFrame?.FrameId)
                .Append(" source=").Append(providerFrame?.SourceId)
                .Append(" sourceConnection=").Append(providerFrame?.Provenance.SourceConnectionToken)
                .Append(" targets=").Append(target.HasValue ? string.Join(",", target.Value.PersonIds) : "unavailable");
            if (providerFrame != null)
            {
                state.Append(" providerPeople=").Append(providerFrame.Players.Count);
                foreach (var player in providerFrame.Players)
                {
                    var center = player.Bounds.Center;
                    bool leftTracked = player.TryGetJoint(PlayerJoint.LeftShoulder, out var left) && left.IsTracked;
                    bool rightTracked = player.TryGetJoint(PlayerJoint.RightShoulder, out var right) && right.IsTracked;
                    state.Append(" p=").Append(player.PersonId)
                        .Append("/score=").Append(player.Confidence.ToString("F2"))
                        .Append("/center=").Append(center.x.ToString("F2")).Append(',').Append(center.y.ToString("F2"))
                        .Append("/shoulders=").Append(leftTracked ? 1 : 0).Append(rightTracked ? 1 : 0);
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            NativeTrackingDiagnostic.AppendFrame(state);
#endif
            Debug.Log("[战斗骨骼链路] " + state);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class NativeTrackingDiagnostic : YouDooSDK.Native.AndroidParseData
        {
            private static readonly int[] m_PersonIds = new int[8];

            public static void AppendFrame(StringBuilder state)
            {
                if (!YoudooGame.SDK.Adapter.YouDooSkeletonSink.IsReady)
                {
                    state.Append(" native=notReady");
                    return;
                }

                try
                {
                    long pointer = GetLatestFrameInfoPtr();
                    if (pointer == 0)
                    {
                        state.Append(" native=noFrame");
                        return;
                    }

                    int detectType = (int)YouDooSDK.Native.YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON;
                    GetFrameInfoBasicByType(pointer, detectType, out long timestamp, out _, out _, out _, out int count);
                    state.Append(" nativeTimeMs=").Append(timestamp).Append(" nativePeople=").Append(count);
                    if (count > 0)
                    {
                        int readCount = GetPersonIdsByType(pointer, detectType, m_PersonIds, m_PersonIds.Length);
                        for (int i = 0; i < readCount && i < m_PersonIds.Length; i++)
                        {
                            state.Append(" nativeId=").Append(m_PersonIds[i]);
                        }
                    }
                }
                catch (Exception exception)
                {
                    state.Append(" nativeError=").Append(exception.GetType().Name);
                }
            }
        }
#endif

        private void FillPerson(TrackedPlayer player, BonePersonData person)
        {
            person.Reset();
            person.m_PersonId = player.PersonId;
            person.m_Body.m_Score = player.Confidence;
            person.m_Body.m_Type = player.PoseType;
            person.m_Body.m_Rect.Set(
                player.Bounds.Left,
                player.Bounds.Top,
                player.Bounds.Right,
                player.Bounds.Bottom);

            for (int jointIndex = 0; jointIndex < BoneTrackedLayout.m_BodyJointCount; jointIndex++)
            {
                if (!player.TryGetJoint((PlayerJoint)jointIndex, out PlayerJointSample sample) || !sample.IsTracked)
                {
                    continue;
                }

                person.m_Body.m_Joints[jointIndex].Set(
                    sample.Position.x,
                    sample.Position.y,
                    sample.Position.z,
                    sample.Confidence);
            }

            FillHand(player.LeftHand, person.m_LeftHand);
            FillHand(player.RightHand, person.m_RightHand);
        }

        private static void FillHand(TrackedHand source, BoneDetectPartData target)
        {
            target.Reset();
            if (source == null)
            {
                return;
            }

            int count = Mathf.Min(source.Joints.Count, target.m_Joints.Length);
            for (int index = 0; index < count; index++)
            {
                PlayerHandJointSample joint = source.Joints[index];
                if (!joint.IsTracked)
                {
                    continue;
                }

                target.m_Joints[index].Set(
                    joint.Position.x,
                    joint.Position.y,
                    joint.Position.z,
                    joint.Confidence);
            }
        }

        private void MarkFrameUnavailable()
        {
            m_LastTrackingFrame = null;
            if (m_LatestFrameData.m_HasFrameData || !m_LatestFrameData.m_HasAuthoritativeSlotBindings)
            {
                ClearFrameData();
            }
        }

        private void ClearFrameData()
        {
            RecycleFramePersons();
            m_LatestFrameData.Reset();
            m_LatestFrameData.m_FrameSerial = ++m_FrameSerial;
            m_LatestFrameData.m_HasAuthoritativeSlotBindings = true;
        }

        private void RecycleFramePersons()
        {
            for (int index = 0; index < m_LatestFrameData.m_Persons.Count; index++)
            {
                BonePersonData person = m_LatestFrameData.m_Persons[index];
                if (person == null)
                {
                    continue;
                }

                person.Reset();
                m_PersonPool.Enqueue(person);
            }
        }

        private BonePersonData RentPersonData()
        {
            return m_PersonPool.Count > 0 ? m_PersonPool.Dequeue() : new BonePersonData();
        }

        private BonePersonData CreateEmptyPersonData()
        {
            BonePersonData person = RentPersonData();
            person.Reset();
            return person;
        }
    }
}
