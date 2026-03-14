using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct FrameEvent
    {
        [JsonConstructor]
        public FrameEvent(string type, int frameIndex, double at)
        {
            Type = type;
            FrameIndex = frameIndex;
            At = at;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("frame")]
        public int FrameIndex { get; }

        // I was kinda confused myself when I first saw the specfification in Speedscope's repo, so this is the timpestamp of the event, idk why it's called at instead of timestamp
        [JsonPropertyName("at")]
        public double At { get; }
    }
}
