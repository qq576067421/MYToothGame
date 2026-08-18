using UnityEngine;

namespace GameDll
{
    public static class BoneFrameJointReader
    {
        public static bool TryReadBodyJoint(BonePersonData person, int jointIndex, float minScore, out Vector2 point)
        {
            float z;
            return TryReadBodyJoint(person, jointIndex, minScore, out point, out z);
        }

        public static bool TryReadBodyJoint(BonePersonData person, int jointIndex, float minScore, out Vector2 point, out float z)
        {
            if (person == null)
            {
                point = Vector2.zero;
                z = 0f;
                return false;
            }

            return TryReadPartJoint(person.m_Body, jointIndex, minScore, out point, out z);
        }

        private static bool TryReadPartJoint(BoneDetectPartData part, int jointIndex, float minScore, out Vector2 point, out float z)
        {
            if (part == null || part.m_Joints == null || jointIndex < 0 || jointIndex >= part.m_Joints.Length)
            {
                point = Vector2.zero;
                z = 0f;
                return false;
            }

            var joint = part.m_Joints[jointIndex];
            if (joint == null || !joint.m_IsTracked || joint.m_Score < minScore)
            {
                point = Vector2.zero;
                z = 0f;
                return false;
            }

            point = new Vector2(joint.m_X, joint.m_Y);
            z = joint.m_Z;
            return true;
        }
    }
}
