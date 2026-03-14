using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using MeowDebugger.API;
using MeowDebugger.API.Features;
using MeowDebugger.API.Features.Speedscope;

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

        string sub = arguments.At(0).ToLowerInvariant();
        var rest = arguments.Skip(1).ToArray();

        switch (sub)
        {
            case "enable":
                GeneralUtils.EnableTool();
                response = "Method metrics enabled.";
                return true;

            case "disable":
                GeneralUtils.DisableTool();
                response = "Method metrics disabled.";
                return true;

            case "top":
                if (rest.Length > 0 && int.TryParse(rest[0], out int topN))
                {
                    response = WithTps(MethodMetrics.ReportAndReset(topN));
                    return true;
                }

                response = "Usage: reporter top <count>";
                return false;

            case "flame":
                string flameName = rest.Length > 0 ? rest[0] : "flamegraph";
                if (!ExportToSpeedscope.ExportJsonFile(out string path))
                {
                    response = "Unable to export speedscope graph.";
                    return false;
                }
                response = $"Flame graph exported to {path}";
                return true;

            case "filter":
                if (rest.Length == 0)
                {
                    response = "Usage: reporter filter <method1> [method2] ...";
                    return false;
                }

                response = WithTps(MethodMetrics.ReportAndReset(rest) ?? "No matching methods.");
                return true;

            case "help":
                response = GetHelp();
                return true;

            default:
                response = $"Unknown subcommand '{sub}'. Use 'reporter help' for usage.";
                return false;
        }
    }

    private static string GetHelp() =>
    "reporter              - Report & reset all metrics\n" +
    "reporter enable       - Enable method metrics\n" +
    "reporter disable      - Disable method metrics\n" +
    "reporter top <N>      - Report top N slowest methods\n" +
    "reporter flame [name] - Export flame graph (optional filename)\n" +
    "reporter filter <...> - Report specific methods by name\n" +
    "reporter help         - Show this help";

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