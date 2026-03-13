using MeowDebugger.API.Features.Speedscope.File.Structs;
using Newtonsoft.Json;
using System.Collections.Generic;


namespace MeowDebugger.API.Features.Speedscope.File
{
    public class SharedFrames
    {
        /// <summary>
        /// The list of <see cref="Frame"/>.
        /// </summary>
        [JsonProperty("frames")]
        public List<Frame> Frames;

        /// <summary>
        /// Creates an instance of <see cref="SharedFrames"/>
        /// </summary>
        /// <param name="frames"><see cref="Frames"/></param>
        public SharedFrames(List<Frame> frames)
        {
            this.Frames = frames;
        }
    }
}
