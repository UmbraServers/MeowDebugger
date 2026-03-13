using Newtonsoft.Json;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public abstract class BaseProfile
    {
        /// <summary>
        /// Name of the profile. Typically, a filename for the source of the profile.
        /// </summary>
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Name of the profile. Typically, a filename for the source of the profile.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        /// <summary>
        /// Unit which all value are specified using in the profile.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; }

        /// <summary>
        /// The starting value of the profile. This will typically be a timestamp.
        /// All event values will be displayed relative to this startValue.
        /// </summary>
        [JsonProperty("startValue")]
        public long StartValue { get; }

        /// <summary>
        /// The final value of the profile. This will typically be a timestamp. This
        /// must be greater than or equal to the startValue. This is useful in
        /// situations where the recorded profile extends past the end of the recorded
        /// events, which may happen if nothing was happening at the end of the
        /// profile.
        /// </summary>
        [JsonProperty("endValue")]
        public long EndValue { get; }

        internal BaseProfile(string Name, string unit, long startValue, long endValue)
        {
            this.Name = Name;
            this.Unit = unit;
            this.EndValue = startValue;
            this.EndValue = endValue;

        }
    }
}
