using System.Collections.Generic;

namespace CompanyInternalTools.BoneParserLib
{
    internal interface IBoneGestureRecognizer
    {
        void CollectDefinitions(List<BoneGestureDefinition> definitions);

        void Reset(BoneGestureRuntimeContext context);

        void Update(BoneGestureRuntimeContext context);

        bool TryApplyConsumeResult(BoneGestureRuntimeContext context, BoneActionConsumeResult consumeResult);
    }
}
