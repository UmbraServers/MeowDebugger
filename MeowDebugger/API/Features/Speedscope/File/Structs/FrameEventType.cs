namespace MeowDebugger.API.Features.Speedscope.File.Structs
{
    public struct FrameEventType
    {
        // This is kinda strange but a guy in Stackoverflow recommended using structs with const strings for "string interfaces", not sure if that's how I should implement it
        public const string OpenFrame = "O";
        public const string CloseFrame = "C";
    }
}
