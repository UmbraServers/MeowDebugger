using MeowDebugger.API.Features.Speedscope.File.Structs;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public class EventedProfile : BaseProfile
    {
        public EventedProfile(string name, string unit, double start, double end, List<FrameEvent> events) : base(name, unit, start, end)
        {
            this.Events = events;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Evented;

        [JsonProperty("events")]
        public List<FrameEvent> Events { get; }
    }
}
