using Newtonsoft.Json;

namespace MeowDebugger.API.Features.Speedscope.File.Events
{
    public abstract class BaseEvent
    {
        [JsonProperty("type")]
        public abstract string Type { get; }

        [JsonProperty("frame")]
        public long FrameIndex { get; }

        [JsonProperty("at")]
        public long At { get; }

        public BaseEvent(long frameIndex, long at)
        {
            FrameIndex = frameIndex;
            At = at;
        }
    }
}
