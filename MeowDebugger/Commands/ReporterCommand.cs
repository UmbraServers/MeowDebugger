using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using MEC;
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

        string command = arguments.At(0).ToLowerInvariant();
        string[] args = arguments.Skip(1).ToArray();

        switch (command)
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
                if (args.Length > 0 && int.TryParse(args[0], out int topN))
                {
                    response = WithTps(MethodMetrics.ReportAndReset(topN));
                    return true;
                }

                response = "Usage: reporter top <count>";
                return false;

            case "flame":
                if (args.Length > 0 && int.TryParse(args[0], out int timer))
                {
                    MethodMetrics.FrameEvents.Clear();
                    MethodMetrics.MethodIndexes.Clear();
                    MethodMetrics.Frames.Clear();

                    Timing.CallDelayed(timer, () =>
                    {
                        if (!ExportToSpeedscope.ExportJsonFile(out string path))
                        {
                            Logger.Error("Unable to export speedscope graph.");
                            return;
                        }
                        Logger.Info($"Speedscope graph exported to {path}");
                    });
                    response = $"Speedscope graph will be exported in {timer} seconds.";
                    return true;
                }

                if (!ExportToSpeedscope.ExportJsonFile(out string path))
                {
                    response = "Unable to export speedscope graph.";
                    return false;
                }
                response = $"Speedscope graph exported to {path}";
                return true;

            case "filter":
                if (args.Length == 0)
                {
                    response = "Usage: reporter filter <method1> [method2] ...";
                    return false;
                }

                response = WithTps(MethodMetrics.ReportAndReset(args) ?? "No matching methods.");
                return true;

            case "help":
                response = GetHelp();
                return true;

            default:
                response = $"Unknown subcommand '{command}'. Use 'reporter help' for usage.";
                return false;
        }
    }

    private static string GetHelp() =>
    "reporter              - Report & reset all metrics\n" +
    "reporter enable       - Enable method metrics\n" +
    "reporter disable      - Disable method metrics\n" +
    "reporter top <N>      - Report top N slowest methods\n" +
    "reporter speed [time] - Exports speedscope graph (optional: profiles data in a certain amount of time)\n" +
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