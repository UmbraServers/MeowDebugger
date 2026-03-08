using System;
using System.Collections.Generic;
using System.IO;
using CommandSystem;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using MeowDebugger.API;
using MeowDebugger.API.Features;

namespace MeowDebugger.Commands;

/// <summary>
/// Command for reporting.
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class ReporterCommand : ICommand
{
    /// <inheritdoc />
    public string Command => "reporter";

    /// <inheritdoc />
    public string[] Aliases => [];

    /// <inheritdoc />
    public string Description => "Reports the slowest methods";

    /// <inheritdoc />
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count <= 0)
        {
            response = WithTps(MethodMetrics.ReportAndReset());
            return true;
        }

        string first = arguments.Array[arguments.Offset]!.ToLowerInvariant();

        if (first == "enable")
        {
            GeneralUtils.EnableTool();
            response = "Method metrics enabled.";
            return true;
        }

        if (first == "disable")
        {
            GeneralUtils.DisableTool();
            response = "Method metrics disabled.";
            return true;
        }

        if (int.TryParse(first, out int result))
        {
            response = WithTps(MethodMetrics.ReportAndReset(result));
            return true;
        }

        if (first.Contains("flame"))
        {
            string path = Path.Combine(PathManager.Configs.FullName, $"{first}.txt");
            MethodMetrics.ExportFlameGraph(path);
            response = $"Flame graph exported to {path}";
            return true;
        }

        var names = new List<string>();
        for (int i = 0; i < arguments.Count; i++)
            names.Add(arguments.Array[arguments.Offset + i]!);

        response = WithTps(MethodMetrics.ReportAndReset(names) ?? "No matching methods.");
        return true;
    }

    private static string WithTps(string? metrics)
    {
        double tps = Server.Tps;
        if (tps > Server.MaxTps)
            tps = Server.MaxTps;
        else if (tps < 0)
            tps = 0;
        return $"TPS: {tps:0.##}\n{metrics}";
    }
}