using MeowDebugger.API.Features.Speedscope.File.Structs;

namespace MeowDebugger.API.Features.Speedscope.File.Events
{
    internal class CloseFrameEvent : BaseEvent
    {
        public CloseFrameEvent(long frameIndex, long at) : base(frameIndex, at)
        {
        }

        public override string Type => EventType.CloseFrame;
    }
}
