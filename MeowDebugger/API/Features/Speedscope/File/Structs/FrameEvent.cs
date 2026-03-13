using Newtonsoft.Json;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct FrameEvent
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("frame")]
        public int FrameIndex;

        [JsonProperty("at")]
        public double At;

        public FrameEvent(string type, int frameIndex, double at)
        {
            Type = type;
            FrameIndex = frameIndex;
            At = at;
        }
    }
}
