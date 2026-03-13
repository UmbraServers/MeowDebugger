namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct ProfileType
    {
        // This is kinda strange but a guy in Stackoverflow recommended using structs with const strings for "string interfaces", not sure if that's how I should implement it
        public const string Evented = "evented";
        public const string Sampled = "sampled";
    }
}
