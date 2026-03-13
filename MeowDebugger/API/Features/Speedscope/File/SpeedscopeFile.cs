using MeowDebugger.API.Features.Speedscope.File.Profiles;
using MeowDebugger.API.Features.Speedscope.File.Structs;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MeowDebugger.API.Features.Speedscope.File
{
    public class SpeedscopeFile
    {
        /// <summary>
        /// The file's schema.
        /// </summary>
        [JsonProperty("$schema")]
        public string Schema = "https://www.speedscope.app/file-format-schema.json";

        /// <summary>
        /// List of profile definitions
        /// </summary>
        [JsonProperty("profiles")]
        public List<BaseProfile> Profiles;

        /// <inheritdoc/>
        [JsonProperty("shared")]
        public SharedFrames Shared;

        /// <summary>
        /// The index into the `profiles` array that should be displayed upon file load. If omitted, will default to displaying the first profile in the file.
        /// </summary>
        [JsonProperty("activeProfileIndex")]
        public int? ActiveProfileIndex;

        /// <summary>
        /// The name of the program which exported this profile. This isn't
        /// consumed but can be helpful for debugging generated data by seeing what
        /// was generating it! Recommended format is "name@version". e.g. when the
        /// file was exported by speedscope v0.6.0 itself, it will be
        /// </summary>
        [JsonProperty("exporter")]
        public string? Exporter = "MeowDebugger@1.1.0";

        /// <summary>
        /// The name of the contained profile group. If omitted, will use the name of the file itself.
        /// </summary>
        [JsonProperty("name")]
        public string? Name;

        /// <summary>
        /// Creates an instance of <see cref="File"/>
        /// </summary>
        /// <param name="shared"><see cref="Shared"/></param>
        /// <param name="profiles"><see cref="Profiles"/></param>
        /// <param name="name"><see cref="Name"/></param>
        public SpeedscopeFile(List<BaseProfile> profiles, SharedFrames shared, string? name)
        {
            Profiles = profiles;
            Shared = shared;
            Name = name;
        }
    }
}
