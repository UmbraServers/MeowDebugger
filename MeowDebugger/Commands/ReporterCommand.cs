using System;
using System.Collections.Generic;
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

        string first = arguments.Array[arguments.Offset]!.ToLowerInvariant();

        if (first == "enable")
        {
            global::MeowDebugger.MeowDebugger.Instance?.EnableTool();
            response = "Method metrics enabled.";
            return true;
        }

        if (first == "disable")
        {
            global::MeowDebugger.MeowDebugger.Instance?.DisableTool();
            response = "Method metrics disabled.";
            return true;
        }

        if (int.TryParse(first, out int result))
        {
            response = MethodMetrics.ReportAndReset(result);
            return true;
        }

        var names = new List<string>();
        for (int i = 0; i < arguments.Count; i++)
            names.Add(arguments.Array[arguments.Offset + i]!);

        response = MethodMetrics.ReportAndReset(names);
        response ??= "No matching methods.";
        return true;
        
        
    }
}