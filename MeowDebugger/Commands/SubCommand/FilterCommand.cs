using CommandSystem;
using MeowDebugger.API.Features;
using System;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class FilterCommand : ICommand
    {
        public string Command => "filter";

        public string[] Aliases => [];

        public string Description => "Filters the reporters output to show only certain methods.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Usage: reporter filter <method1> [method2] ...";
                return false;
            }

            response = ReporterParentCommand.WithTps(MethodMetrics.ReportAndReset(arguments) ?? "No matching methods.");
            return true;
        }
    }
}
