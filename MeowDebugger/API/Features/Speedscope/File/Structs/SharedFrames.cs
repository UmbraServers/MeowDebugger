using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct SharedFrames
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

