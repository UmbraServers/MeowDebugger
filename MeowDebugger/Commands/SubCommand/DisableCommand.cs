using CommandSystem;
using MeowDebugger.API;
using System;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class DisableCommand : ICommand
    {
        public string Command => "disable";

        public string[] Aliases => [];

        public string Description => "Disables the debugger tool.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            GeneralUtils.DisableTool();
            response = "Method metrics disabled.";
            return true;
        }
    }
}
