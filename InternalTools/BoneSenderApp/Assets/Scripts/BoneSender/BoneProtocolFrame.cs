using System;

namespace BoneSender
{
    [Serializable]
    public sealed class BoneProtocolFrame
    {
        public int m_ProtocolVersion = 1;
        public string m_SessionId = string.Empty;
        public int m_FrameSerial;
        public bool m_IsSimulated;
        public long m_CaptureTimeMs;
        public int m_ImageWidth;
        public int m_ImageHeight;
        public BoneProtocolPerson[] m_Persons = Array.Empty<BoneProtocolPerson>();
    }

    [Serializable]
    public sealed class BoneProtocolPerson
    {
        public int m_PersonId = YouDooSDKConstants.PersonIdNull;
        public BoneProtocolPart m_Body = new BoneProtocolPart();
        public BoneProtocolPart m_LeftHand = new BoneProtocolPart();
        public BoneProtocolPart m_RightHand = new BoneProtocolPart();
        public BoneProtocolPart m_Face = new BoneProtocolPart();
    }

    [Serializable]
    public sealed class BoneProtocolPart
    {
        public float m_Score;
        public int m_Type;
        public BoneProtocolRect m_Rect = new BoneProtocolRect();
        public BoneProtocolJoint[] m_Joints = Array.Empty<BoneProtocolJoint>();
    }

    [Serializable]
    public sealed class BoneProtocolRect
    {
        public bool m_IsTracked;
        public float m_Left;
        public float m_Top;
        public float m_Right;
        public float m_Bottom;
    }

    [Serializable]
    public sealed class BoneProtocolJoint
    {
        public bool m_IsTracked;
        public float m_X;
        public float m_Y;
        public float m_Z;
        public float m_Score;
    }
}
