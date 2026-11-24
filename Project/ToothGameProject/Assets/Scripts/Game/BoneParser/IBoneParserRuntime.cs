using System.Collections.Generic;

namespace GameDll
{
    internal interface IBoneParserRuntime
    {
        string RuntimeName { get; }

        void Reset();

        void ResetSeat(int seatIndex);

        BoneParserFrameResult Update(BoneTrackedFrame frameData, IList<BoneParserSeatDefinition> seatDefinitions);

        bool ApplyActionConsumeResult(BoneActionConsumeResult consumeResult);

        void Shutdown();
    }
}
