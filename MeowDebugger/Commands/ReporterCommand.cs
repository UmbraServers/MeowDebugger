using System;
using CommandSystem;
using MeowDebugger.API.Features;

namespace MeowDebugger.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class ReporterCommand : ICommand
{
    public string Command => "reporter";

    public string[] Aliases => [];

    public string Description => "Reports the slowest methods";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count <= 0)
        {
            response = MethodMetrics.ReportAndReset();
            return true;
        }

        if (!int.TryParse(arguments.At(0), out int result))
        {
            response = MethodMetrics.ReportAndReset();
            return true;
        }
        
        response = MethodMetrics.ReportAndReset(result);
        return true;
    }
}