using MeowDebugger.API.Features.Speedscope.File.Events;
using MeowDebugger.API.Features.Speedscope.File.Structs;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public class EventedProfile : BaseProfile
    {
        public EventedProfile(string name, string unit, long start, long end, List<BaseEvent> events) : base(name, unit, start, end)
        {
            this.Events = events;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Evented;

        [JsonProperty("events")]
        public List<BaseEvent> Events { get; }
    }
}
