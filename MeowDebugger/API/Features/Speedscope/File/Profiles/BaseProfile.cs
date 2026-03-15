using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    /// <summary>
    /// Serves as the abstract base class for different types of profiles, providing common properties and structure for
    /// derived profile types.
    /// </summary>
    [JsonDerivedType(typeof(EventedProfile))]
    [JsonDerivedType(typeof(SampledProfile))]
    public abstract class BaseProfile
    {
        // TODO: Maybe make this a generic, seems more appropriate idk

        /// <summary>
        /// Initializes a new instance of <see cref="EventedProfile"/>.
        /// </summary>
        /// <param name="name">The unique name that identifies the profile.</param>
        /// <param name="unit">The unit of measurement for the profile's range, such as 'seconds' or 'meters'.</param>
        /// <param name="startValue">The starting value of the profile's range.</param>
        /// <param name="endValue">The ending value of the profile's range.</param>
        internal BaseProfile(string name, string unit, double startValue, double endValue)
        {
            this.Name = name;
            this.Unit = unit;
            this.StartValue = startValue;
            this.EndValue = endValue;
        }

        /// <summary>
        /// Name of the profile. Typically, a filename for the source of the profile.
        /// </summary>
        [JsonPropertyName("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Name of the profile. Typically, a filename for the source of the profile.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; }

        /// <summary>
        /// Unit which all value are specified using in the profile.
        /// </summary>
        [JsonPropertyName("unit")]
        public string Unit { get; }

        /// <summary>
        /// The starting value of the profile. This will typically be a timestamp.
        /// All event values will be displayed relative to this startValue.
        /// </summary>
        [JsonPropertyName("startValue")]
        public double StartValue { get; }

        /// <summary>
        /// The final value of the profile. This will typically be a timestamp. This
        /// must be greater than or equal to the startValue. This is useful in
        /// situations where the recorded profile extends past the end of the recorded
        /// events, which may happen if nothing was happening at the end of the
        /// profile.
        /// </summary>
        [JsonPropertyName("endValue")]
        public double EndValue { get; }
    }
}
