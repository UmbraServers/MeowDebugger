using Newtonsoft.Json;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    /// <summary>
    /// Represents the struct for the <see cref="Frame"/>.
    /// </summary>
    public struct Frame
    {
        /// <summary>
        /// The frame's name.
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("file")]
        public string File;

        /// <summary>
        /// Creates an instance of <see cref="Frame"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="file"></param>
        public Frame(string name, string file)
        {
            this.Name = name;
            this.File = file;
        }
    }
}
