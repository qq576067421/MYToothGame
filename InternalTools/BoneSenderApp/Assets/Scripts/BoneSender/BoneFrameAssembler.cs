using System;
using System.Collections.Generic;

namespace BoneSender
{
    public sealed class BoneFrameAssembler
    {
        private readonly BoneProtocolFrame m_Frame = new BoneProtocolFrame();
        private readonly List<BoneProtocolPerson> m_PersonBuffer = new List<BoneProtocolPerson>();

        public BoneProtocolFrame BeginFrame(string sessionId, int frameSerial, long captureTimeMs, int imageWidth, int imageHeight)
        {
            m_PersonBuffer.Clear();
            m_Frame.m_ProtocolVersion = 1;
            m_Frame.m_SessionId = sessionId ?? string.Empty;
            m_Frame.m_FrameSerial = frameSerial;
            m_Frame.m_CaptureTimeMs = captureTimeMs;
            m_Frame.m_ImageWidth = imageWidth;
            m_Frame.m_ImageHeight = imageHeight;
            m_Frame.m_Persons = Array.Empty<BoneProtocolPerson>();
            return m_Frame;
        }

        public BoneProtocolPerson AddPerson(int personId)
        {
            var person = new BoneProtocolPerson();
            person.m_PersonId = personId;
            m_PersonBuffer.Add(person);
            return person;
        }

        public BoneProtocolPart CreatePart(int jointCount)
        {
            var part = new BoneProtocolPart();
            if (jointCount <= 0)
            {
                part.m_Joints = Array.Empty<BoneProtocolJoint>();
                return part;
            }

            part.m_Joints = new BoneProtocolJoint[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                part.m_Joints[i] = new BoneProtocolJoint();
            }

            return part;
        }

        public BoneProtocolFrame EndFrame()
        {
            m_Frame.m_Persons = m_PersonBuffer.ToArray();
            return m_Frame;
        }
    }
}
