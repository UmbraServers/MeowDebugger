using MeowDebugger.API.Features.Speedscope.File.Structs;

namespace MeowDebugger.API.Features.Speedscope.File.Events
{
    public class OpenFrameEvent : BaseEvent
    {
        public OpenFrameEvent(long frameIndex, long at) : base(frameIndex, at)
        {
        }

        public override string Type => EventType.OpenFrame;
    }
}
