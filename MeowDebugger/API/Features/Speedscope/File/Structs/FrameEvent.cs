using Newtonsoft.Json;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct FrameEvent
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("frame")]
        public int FrameIndex;

        // I was kinda confused myself when I first saw the specfification in Speedscope's repo, so this is the timpestamp of the event, idk why it's called at instead of timestamp
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
