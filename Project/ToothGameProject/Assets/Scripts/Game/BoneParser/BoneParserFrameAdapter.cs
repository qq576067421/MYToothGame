using System.Collections.Generic;

namespace GameDll
{
    internal sealed class BoneParserFrameAdapter
    {
        private readonly BoneTrackedFrame m_TargetFrame = new BoneTrackedFrame();
        private readonly Queue<BoneTrackedPerson> m_PersonPool = new Queue<BoneTrackedPerson>();

        public BoneTrackedFrame Convert(BoneFrameData sourceFrame)
        {
            RecycleFramePersons();
            m_TargetFrame.Reset();
            if (sourceFrame == null)
            {
                return m_TargetFrame;
            }

            m_TargetFrame.m_HasFrameData = sourceFrame.m_HasFrameData;
            m_TargetFrame.m_FrameSerial = sourceFrame.m_FrameSerial;
            m_TargetFrame.m_IsSimulated = sourceFrame.m_IsSimulated;
            m_TargetFrame.m_FrameTimeMs = sourceFrame.m_FrameTimeMs;
            m_TargetFrame.m_ImageWidth = sourceFrame.m_ImageWidth;
            m_TargetFrame.m_ImageHeight = sourceFrame.m_ImageHeight;

            for (int i = 0; i < sourceFrame.m_Persons.Count; i++)
            {
                BoneTrackedPerson targetPerson = RentPerson();
                CopyPerson(sourceFrame.m_Persons[i], targetPerson);
                m_TargetFrame.m_Persons.Add(targetPerson);
            }

            return m_TargetFrame;
        }

        private static void CopyJoint(BoneJointData sourceJoint, BoneTrackedJoint targetJoint)
        {
            targetJoint.Reset();
            if (sourceJoint == null || !sourceJoint.m_IsTracked)
            {
                return;
            }

            targetJoint.Set(sourceJoint.m_X, sourceJoint.m_Y, sourceJoint.m_Z, sourceJoint.m_Score);
        }

        private static void CopyPart(BoneDetectPartData sourcePart, BoneTrackedPart targetPart)
        {
            targetPart.Reset();
            if (sourcePart == null)
            {
                return;
            }

            targetPart.m_Rect.Set(
                sourcePart.m_Rect.m_Left,
                sourcePart.m_Rect.m_Top,
                sourcePart.m_Rect.m_Right,
                sourcePart.m_Rect.m_Bottom);
            targetPart.m_Score = sourcePart.m_Score;
            targetPart.m_Type = sourcePart.m_Type;

            int jointCount = sourcePart.m_Joints != null
                ? sourcePart.m_Joints.Length
                : 0;
            int copyCount = jointCount < targetPart.m_Joints.Length ? jointCount : targetPart.m_Joints.Length;
            for (int i = 0; i < copyCount; i++)
            {
                CopyJoint(sourcePart.m_Joints[i], targetPart.m_Joints[i]);
            }
        }

        private static void CopyPerson(BonePersonData sourcePerson, BoneTrackedPerson targetPerson)
        {
            targetPerson.Reset();
            if (sourcePerson == null)
            {
                return;
            }

            targetPerson.m_PersonId = sourcePerson.m_PersonId;
            CopyPart(sourcePerson.m_Body, targetPerson.m_Body);
            CopyPart(sourcePerson.m_LeftHand, targetPerson.m_LeftHand);
            CopyPart(sourcePerson.m_RightHand, targetPerson.m_RightHand);
            CopyPart(sourcePerson.m_Face, targetPerson.m_Face);
        }

        private void RecycleFramePersons()
        {
            for (int i = 0; i < m_TargetFrame.m_Persons.Count; i++)
            {
                BoneTrackedPerson person = m_TargetFrame.m_Persons[i];
                if (person == null)
                {
                    continue;
                }

                person.Reset();
                m_PersonPool.Enqueue(person);
            }
        }

        private BoneTrackedPerson RentPerson()
        {
            return m_PersonPool.Count > 0 ? m_PersonPool.Dequeue() : new BoneTrackedPerson();
        }
    }
}
