using System;
using System.Collections.Generic;
using UnityEngine;
using YoudooGame.Core;
using YoudooGame.Framework;
using YoudooGame.MatchFlow.Runtime;

namespace GameDll
{
    public sealed class BattleBoneParseData : MonoBehaviour, IBoneFrameSource
    {
        private readonly BoneFrameData m_LatestFrameData = new BoneFrameData();
        private readonly Queue<BonePersonData> m_PersonPool = new Queue<BonePersonData>();
        private int m_ConfiguredPlayerCount = BoneSlotLayout.m_SlotCount;
        private int m_FrameSerial;
        private long m_LastFilledTrackingFrameId = long.MinValue;

        public BoneFrameData ReadLatestFrameData()
        {
            return m_LatestFrameData;
        }

        public bool TryReadLockedPersonId(int slotIndex, out int personId)
        {
            personId = BoneTrackedLayout.m_InvalidPersonId;
            if (slotIndex < 0 || slotIndex >= m_ConfiguredPlayerCount ||
                !YoudooAPI.TryGetSessionSnapshot(out MatchSessionSnapshot snapshot) ||
                snapshot == null || !snapshot.TryGetBoundPlayer(slotIndex, out TrackedPlayer player) ||
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

        public void ConfigureBattleSlotLayout(int playerCount, int[] initialPersonIds)
        {
            m_ConfiguredPlayerCount = Mathf.Clamp(playerCount, 1, BoneSlotLayout.m_SlotCount);
            MarkFrameUnavailable();
        }

        private void OnEnable()
        {
            enabled = true;
        }

        private void Update()
        {
            if (!YoudooAPI.TryGetSessionSnapshot(out MatchSessionSnapshot snapshot) ||
                snapshot == null ||
                snapshot.LifecycleState != MatchSessionLifecycleState.Running ||
                snapshot.FrameState != MatchSessionFrameState.Current ||
                snapshot.DataChain == null || snapshot.DataChain.CurrentTrackingFrame == null)
            {
                MarkFrameUnavailable();
                return;
            }

            var trackingFrame = snapshot.DataChain.CurrentTrackingFrame;

            // 同一原生骨骼帧只填充一次：帧号未变时数据完全一致，
            // 重复走槽位解析与 FillPerson 只会白耗 CPU 与 GC。
            if (m_LastFilledTrackingFrameId == trackingFrame.FrameId)
            {
                return;
            }

            m_LastFilledTrackingFrameId = trackingFrame.FrameId;
            RecycleFramePersons();
            m_LatestFrameData.Reset();
            m_LatestFrameData.m_HasFrameData = true;
            m_LatestFrameData.m_FrameSerial = unchecked((int)trackingFrame.FrameId);
            m_LatestFrameData.m_FrameTimeMs = (long)(trackingFrame.TimestampSec * 1000d);

            for (int slotIndex = 0; slotIndex < m_ConfiguredPlayerCount; slotIndex++)
            {
                if (snapshot.TryGetOperablePlayer(slotIndex, out TrackedPlayer player) && player != null)
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
        }

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
            m_LastFilledTrackingFrameId = long.MinValue;
            RecycleFramePersons();
            m_LatestFrameData.Reset();
            m_LatestFrameData.m_FrameSerial = ++m_FrameSerial;
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
