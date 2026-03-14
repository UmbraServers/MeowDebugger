using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    /// <summary>
    /// Represents an event associated with a frame, including its type, index, and timestamp.
    /// </summary>
    // Idk I'm going to add readonly even thought I'm pretty sure structs are readonly by default.
    public readonly struct FrameEvent
    {
        /// <summary>
        /// Initializes a new instance a <see cref="FrameEvent"/>.
        /// </summary>
        /// <param name="type">The type of the frame event, indicating the nature or category of the event.</param>
        /// <param name="frameIndex">The zero-based index of the frame within the sequence, representing the event's position in the timeline.</param>
        /// <param name="at">The timestamp, in seconds, at which the frame event occurs.</param>
        [JsonConstructor]
        public FrameEvent(string type, int frameIndex, double at)
        {
            Type = type;
            FrameIndex = frameIndex;
            At = at;
        }

        /// <summary>
        /// Gets the type of the <see cref="FrameEvent"/>, which can be either "O" for open or "C" for close.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; }

        /// <summary>
        /// Gets the index of the <see cref="Frame"/> in <see cref="MethodMetrics.FrameEvents"/>.
        /// </summary>
        [JsonPropertyName("frame")]
        public int FrameIndex { get; }

        /// <summary>
        /// Gets the timestamp indicating when the event occurred.
        /// </summary>
        [JsonPropertyName("at")]
        public double At { get; }
    }
}
