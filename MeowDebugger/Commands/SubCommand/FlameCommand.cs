using CommandSystem;
using LabApi.Features.Console;
using MEC;
using MeowDebugger.API.Features;
using MeowDebugger.API.Features.Speedscope;
using System;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class FlameCommand : ICommand
    {
        public string Command => "flame";

        public string[] Aliases => [];

        public string Description => "Generates a flamegraph compatible with Speedscope.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count > 0 && int.TryParse(arguments.At(0), out int timer))
            {
                foreach (var frameEvents in MethodMetrics.FrameEvents.Values)
                {
                    frameEvents.Events.Clear();
                }
                
                MethodMetrics.MethodIndexes.Clear();
                MethodMetrics.Frames.Clear();

                Timing.CallDelayed(timer, () =>
                {
                    if (!ExportToSpeedscope.ExportJsonFile(out string path))
                    {
                        Logger.Error("Unable to export speedscope graph.");
                        return;
                    }
                    
                    Logger.Info($"Speedscope graph exported to {path}");
                });

                response = $"Speedscope graph will be exported in {timer} seconds.";
                return true;
            }

            if (!ExportToSpeedscope.ExportJsonFile(out string path))
            {
                response = "Unable to export speedscope graph.";
                return false;
            }

            response = $"Speedscope graph exported to {path}";
            return true;
        }
    }
}
