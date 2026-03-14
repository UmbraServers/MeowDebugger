using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    /// <summary>
    /// Represents the struct for the <see cref="Frame"/>.
    /// </summary>
    public struct Frame
    {
        /// <summary>
        /// Creates an instance of <see cref="Frame"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="file"></param>
        [JsonConstructor]
        public Frame(string name, string file)
        {
            this.Name = name;
            this.File = file;
        }

        /// <summary>
        /// The frame's name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("file")]
        public string File { get; }
    }
}
