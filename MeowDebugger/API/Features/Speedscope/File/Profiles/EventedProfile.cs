using MeowDebugger.API.Features.Speedscope.File.Structs;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    /// <summary>
    /// Represents a profile that contains a collection of frame events, extending the base profile functionality to
    /// support event-driven profiling data.
    /// </summary>
    public class EventedProfile : BaseProfile
    {
        /// <inheritdoc/>
        [JsonConstructor]
        public EventedProfile(string name, string unit, double startValue, double endValue, List<FrameEvent> events) : base(name, unit, startValue, endValue)
        {
            this.Events = events;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Evented;

        /// <summary>
        /// Gets a list for <see cref="FrameEvent"/>.
        /// </summary>
        [JsonPropertyName("events")]
        public List<FrameEvent> Events { get; }
    }
}
