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
            response = $"Disabled in this build.";
            return false;
        }
    }
}
