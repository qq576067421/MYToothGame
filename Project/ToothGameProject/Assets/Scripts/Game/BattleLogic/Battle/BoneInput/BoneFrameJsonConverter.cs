using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public sealed class BoneFrameJsonConverter
    {
        [Serializable]
        private sealed class FrameDto
        {
            public int m_ProtocolVersion;
            public string m_SessionId;
            public int m_FrameSerial;
            public bool m_IsSimulated;
            public long m_CaptureTimeMs;
            public int m_ImageWidth;
            public int m_ImageHeight;
            public PersonDto[] m_Persons;
        }

        [Serializable]
        private sealed class PersonDto
        {
            public int m_PersonId;
            public PartDto m_Body;
            public PartDto m_LeftHand;
            public PartDto m_RightHand;
            public PartDto m_Face;
        }

        [Serializable]
        private sealed class PartDto
        {
            public float m_Score;
            public int m_Type;
            public RectDto m_Rect;
            public JointDto[] m_Joints;
        }

        [Serializable]
        private sealed class RectDto
        {
            public bool m_IsTracked;
            public float m_Left;
            public float m_Top;
            public float m_Right;
            public float m_Bottom;
        }

        [Serializable]
        private sealed class JointDto
        {
            public bool m_IsTracked;
            public float m_X;
            public float m_Y;
            public float m_Z;
            public float m_Score;
        }

        private readonly BoneFrameData m_FrameData = new BoneFrameData();
        private readonly Queue<BonePersonData> m_PersonPool = new Queue<BonePersonData>();

        public BoneFrameData Convert(string json)
        {
            RecycleFramePersons();
            m_FrameData.Reset();

            if (string.IsNullOrEmpty(json))
            {
                return m_FrameData;
            }

            var dto = JsonUtility.FromJson<FrameDto>(json);
            if (dto == null)
            {
                return m_FrameData;
            }

            BuildFrame(dto);
            return m_FrameData;
        }

        private void BuildFrame(FrameDto dto)
        {
            m_FrameData.m_HasFrameData = true;
            m_FrameData.m_FrameSerial = dto.m_FrameSerial;
            m_FrameData.m_IsSimulated = dto.m_IsSimulated;
            m_FrameData.m_FrameTimeMs = dto.m_CaptureTimeMs;
            m_FrameData.m_ImageWidth = dto.m_ImageWidth;
            m_FrameData.m_ImageHeight = dto.m_ImageHeight;

            if (dto.m_Persons == null)
            {
                return;
            }

            for (int i = 0; i < dto.m_Persons.Length; i++)
            {
                var personData = RentPersonData();
                personData.Reset();
                var personDto = dto.m_Persons[i];
                if (personDto == null || personDto.m_PersonId == YouDooSDKConstants.PersonIdNull)
                {
                    m_FrameData.m_Persons.Add(personData);
                    continue;
                }

                personData.m_PersonId = personDto.m_PersonId;
                FillPart(personData.m_Body, personDto.m_Body);
                FillPart(personData.m_LeftHand, personDto.m_LeftHand);
                FillPart(personData.m_RightHand, personDto.m_RightHand);
                FillPart(personData.m_Face, personDto.m_Face);
                m_FrameData.m_Persons.Add(personData);
            }
        }

        private void FillPart(BoneDetectPartData targetPart, PartDto sourcePart)
        {
            if (targetPart == null || sourcePart == null)
            {
                targetPart?.Reset();
                return;
            }

            targetPart.Reset();
            targetPart.m_Score = sourcePart.m_Score;
            targetPart.m_Type = sourcePart.m_Type;

            if (sourcePart.m_Rect != null && sourcePart.m_Rect.m_IsTracked)
            {
                targetPart.m_Rect.Set(
                    sourcePart.m_Rect.m_Left,
                    sourcePart.m_Rect.m_Top,
                    sourcePart.m_Rect.m_Right,
                    sourcePart.m_Rect.m_Bottom);
            }

            if (sourcePart.m_Joints == null)
            {
                return;
            }

            int jointCount = Mathf.Min(targetPart.m_Joints.Length, sourcePart.m_Joints.Length);
            for (int i = 0; i < jointCount; i++)
            {
                var jointDto = sourcePart.m_Joints[i];
                if (jointDto == null || !jointDto.m_IsTracked)
                {
                    continue;
                }

                targetPart.m_Joints[i].Set(jointDto.m_X, jointDto.m_Y, jointDto.m_Z, jointDto.m_Score);
            }
        }

        private BonePersonData RentPersonData()
        {
            return m_PersonPool.Count > 0 ? m_PersonPool.Dequeue() : new BonePersonData();
        }

        private void RecycleFramePersons()
        {
            for (int i = 0; i < m_FrameData.m_Persons.Count; i++)
            {
                var personData = m_FrameData.m_Persons[i];
                if (personData == null)
                {
                    continue;
                }

                personData.Reset();
                m_PersonPool.Enqueue(personData);
            }
        }
    }
}
