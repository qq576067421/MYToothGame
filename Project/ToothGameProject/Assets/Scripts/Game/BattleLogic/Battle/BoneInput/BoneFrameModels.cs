using System.Collections.Generic;

namespace GameDll
{
    public sealed class BoneJointData
    {
        public bool m_IsTracked;
        public float m_X;
        public float m_Y;
        public float m_Z;
        public float m_Score;

        public void Reset()
        {
            m_IsTracked = false;
            m_X = 0f;
            m_Y = 0f;
            m_Z = 0f;
            m_Score = 0f;
        }

        public void Set(float x, float y, float z, float score)
        {
            m_IsTracked = true;
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_Score = score;
        }
    }

    public sealed class BoneRectData
    {
        public bool m_IsValid;
        public float m_Left;
        public float m_Top;
        public float m_Right;
        public float m_Bottom;

        public void Reset()
        {
            m_IsValid = false;
            m_Left = 0f;
            m_Top = 0f;
            m_Right = 0f;
            m_Bottom = 0f;
        }

        public void Set(float left, float top, float right, float bottom)
        {
            m_IsValid = right > left && bottom > top;
            m_Left = left;
            m_Top = top;
            m_Right = right;
            m_Bottom = bottom;
        }

        public float ReadCenterX()
        {
            return (m_Left + m_Right) * 0.5f;
        }

        public float ReadCenterY()
        {
            return (m_Top + m_Bottom) * 0.5f;
        }

        public float ReadWidth()
        {
            return UnityEngine.Mathf.Max(0f, m_Right - m_Left);
        }

        public float ReadHeight()
        {
            return UnityEngine.Mathf.Max(0f, m_Bottom - m_Top);
        }
    }

    public sealed class BoneDetectPartData
    {
        public readonly BoneRectData m_Rect;
        public readonly BoneJointData[] m_Joints;
        public float m_Score;
        public int m_Type;

        public BoneDetectPartData(int jointCount)
        {
            m_Rect = new BoneRectData();
            m_Joints = new BoneJointData[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                m_Joints[i] = new BoneJointData();
            }
        }

        public void Reset()
        {
            m_Rect.Reset();
            m_Score = 0f;
            m_Type = 0;
            for (int i = 0; i < m_Joints.Length; i++)
            {
                m_Joints[i].Reset();
            }
        }
    }

    public sealed class BonePersonData
    {
        public int m_PersonId = YouDooSDKConstants.PersonIdNull;
        public readonly BoneDetectPartData m_Body;
        public readonly BoneDetectPartData m_LeftHand;
        public readonly BoneDetectPartData m_RightHand;
        public readonly BoneDetectPartData m_Face;

        public BonePersonData()
        {
            m_Body = new BoneDetectPartData((int)YouDooSDKConstants.KeyPointIndex.KEYPOINT_COUNT);
            m_LeftHand = new BoneDetectPartData((int)YouDooSDKConstants.HandLandmark21.HAND_LANDMARK_COUNT);
            m_RightHand = new BoneDetectPartData((int)YouDooSDKConstants.HandLandmark21.HAND_LANDMARK_COUNT);
            m_Face = new BoneDetectPartData((int)YouDooSDKConstants.FaceLandmark5.FACE_LANDMARK_COUNT);
        }

        public void Reset()
        {
            m_PersonId = YouDooSDKConstants.PersonIdNull;
            m_Body.Reset();
            m_LeftHand.Reset();
            m_RightHand.Reset();
            m_Face.Reset();
        }
    }

    public sealed class BoneFrameData
    {
        public bool m_HasFrameData;
        public int m_FrameSerial;
        public bool m_IsSimulated;
        public long m_FrameTimeMs;
        public int m_ImageWidth;
        public int m_ImageHeight;
        public readonly List<BonePersonData> m_Persons = new List<BonePersonData>();

        public void Reset()
        {
            m_HasFrameData = false;
            m_FrameSerial = 0;
            m_IsSimulated = false;
            m_FrameTimeMs = 0;
            m_ImageWidth = 0;
            m_ImageHeight = 0;
            m_Persons.Clear();
        }
    }
}
