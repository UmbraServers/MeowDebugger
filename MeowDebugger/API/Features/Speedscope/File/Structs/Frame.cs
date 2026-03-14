using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    /// <summary>
    /// Represents the struct for the <see cref="Frame"/>.
    /// </summary>
    public readonly struct Frame
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
        /// Gets the  <see cref="Frame"/>'s name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; }

        /// <summary>
        /// Gets the name of the file associated with <see cref="Frame"/>.
        /// </summary>
        [JsonPropertyName("file")]
        public string File { get; }
    }
}
