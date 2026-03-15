using CommandSystem;
using System;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class HelpCommand : ICommand
    {
        public string Command => "help";

        public string[] Aliases => [];

        public string Description => "Usage of the reporter command.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response =
                "\nreporter              - Report & reset all metrics\n" +
                "reporter enable       - Enable method metrics\n" +
                "reporter disable      - Disable method metrics\n" +
                "reporter top <N>      - Report top N slowest methods\n" +
                "reporter flame <time> - Exports speedscope graph (optional: profiles in a certain amount of time)\n" +
                "reporter filter <...> - Report specific methods by name\n" +
                "reporter help         - Show this help\n" + 
                "reporter tps <time>   - Measures TPS in a certain amount of time";
            return true;
        }
    }
}
