using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct SharedFrames
    {
        /// <summary>
        /// Creates an instance of <see cref="SharedFrames"/>
        /// </summary>
        /// <param name="frames"><see cref="Frames"/></param>
        [JsonConstructor]
        public SharedFrames(List<Frame> frames)
        {
            this.Frames = frames;
        }

        /// <summary>
        /// The list of <see cref="Frame"/>.
        /// </summary>
        [JsonPropertyName("frames")]
        public List<Frame> Frames { get; }
    }
}

