using CommandSystem;
using MeowDebugger.API;
using System;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class EnableCommand : ICommand
    {
        public string Command => "enable";

        public string[] Aliases => [];

        public string Description => "Enables the debugger tool.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            GeneralUtils.EnableTool();
            response = "Method metrics enabled.";
            return true;
        }
    }
}
