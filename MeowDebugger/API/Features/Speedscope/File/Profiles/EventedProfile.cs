using MeowDebugger.API.Features.Speedscope.File.Structs;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public class EventedProfile : BaseProfile
    {
        [JsonConstructor]
        public EventedProfile(string name, string unit, double start, double end, List<FrameEvent> events) : base(name, unit, start, end)
        {
            this.Events = events;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Evented;

        [JsonPropertyName("events")]
        public List<FrameEvent> Events { get; }
    }
}
