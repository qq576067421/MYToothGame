using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal sealed class BoneGestureRecognizerRegistry
    {
        private readonly List<IBoneGestureRecognizer> m_Recognizers = new List<IBoneGestureRecognizer>();

        public static BoneGestureRecognizerRegistry CreateDefault()
        {
            BoneGestureRecognizerRegistry registry = new BoneGestureRecognizerRegistry();
            registry.Register(new BonePoseGestureRecognizer());
            registry.Register(new AlternatingSwingFlowRecognizer());
            registry.Register(new LargeAlternatingSwingFlowRecognizer());
            registry.Register(new OverheadPressReleaseFlowRecognizer());
            registry.Register(new CrossChestExpandFlowRecognizer());
            registry.Register(new SingleHandPullDownFlowRecognizer());
            registry.Register(new HandsOnHipRaiseFlowRecognizer());
            registry.Register(new CrouchStandRaiseFlowRecognizer());
            registry.Register(new ChestClosePushFlowRecognizer());
            registry.Register(new HandsExpandHoldFlowRecognizer());
            return registry;
        }

        public static void CollectDefaultDefinitions(List<BoneGestureDefinition> definitions)
        {
            CreateDefault().CollectDefinitions(definitions);
        }

        public void Register(IBoneGestureRecognizer recognizer)
        {
            if (recognizer == null)
            {
                return;
            }

            m_Recognizers.Add(recognizer);
        }

        public void CollectDefinitions(List<BoneGestureDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < m_Recognizers.Count; i++)
            {
                m_Recognizers[i].CollectDefinitions(definitions);
            }
        }

        public void Reset(BoneGestureRuntimeContext context)
        {
            for (int i = 0; i < m_Recognizers.Count; i++)
            {
                m_Recognizers[i].Reset(context);
            }
        }

        public void Update(BoneGestureRuntimeContext context)
        {
            for (int i = 0; i < m_Recognizers.Count; i++)
            {
                m_Recognizers[i].Update(context);
            }
        }

        public bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult)
        {
            for (int i = 0; i < m_Recognizers.Count; i++)
            {
                if (m_Recognizers[i].TryApplyConsumeResult(context, consumeResult))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
