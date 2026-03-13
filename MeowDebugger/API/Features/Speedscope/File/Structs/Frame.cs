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
        public string name;

        /// <summary>
        /// Creates an instance of <see cref="Frame"/>
        /// </summary>
        /// <param name="name"></param>
        public Frame(string name)
        {
            this.name = name;
        }
    }
}
