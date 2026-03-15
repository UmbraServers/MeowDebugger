using CommandSystem;
using MeowDebugger.API.Features;
using System;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class TopCommand : ICommand
    {
        public string Command => "top";

        public string[] Aliases => [];

        public string Description => "Reports the top laggiest methods.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count > 0 && int.TryParse(arguments.At(0), out int topN))
            {
                response = $"TPS: {MethodMetrics.GetClampedTps():0.##}\n{MethodMetrics.ReportAndReset(topN)}";
                return true;
            }

            response = "Usage: reporter top <count>";
            return false;
        }
    }
}
