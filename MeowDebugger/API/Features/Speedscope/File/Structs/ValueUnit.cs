using MeowDebugger.API.Features.Speedscope.File.Profiles;

namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    /// <summary>
    /// Represents the enumaration for the value unit of the <see cref="BaseProfile"/>.
    /// </summary>
    public struct ValueUnit
    {
        public const string Bytes = "bytes";
        public const string Microseconds = "microseconds";
        public const string Milliseconds = "milliseconds";
        public const string Nanoseconds = "nanoseconds";
        public const string Seconds = "seconds";
        public const string None = "None";
    }
}
