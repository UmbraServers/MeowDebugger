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
            response = $"Disabled in this build.";
            return false;
        }
    }
}
