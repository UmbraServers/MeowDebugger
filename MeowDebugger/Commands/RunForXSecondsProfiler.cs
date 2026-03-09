using CommandSystem;
using LabApi.Features.Console;
using MeowDebugger.API.Features;
using NetworkManagerUtils.Dummies;
using System;
using System.IO;
using LabApi.Loader.Features.Paths;

namespace MeowDebugger.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class RunForXSecondsProfiler : ICommand
{
    public string Command => "profile";

    public string[] Aliases => [];

    public string Description => "Generates a flamegraph.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count <= 0)
        {
            response = "Usage: <seconds> [filename]";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int delay) || delay <= 0)
        {
            response = "Please pass a valid positive number for seconds.";
            return false;
        }

        string fileName = arguments.Count > 1
            ? arguments.At(1).Trim()
            : "flame-generated";

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "flame-generated";

        fileName = Path.GetFileName(fileName);
        if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            fileName += ".txt";

        string path = Path.Combine(PathManager.Configs.FullName, fileName);

        MEC.Timing.CallDelayed(delay, () =>
        {
            MethodMetrics.ExportFlameGraph(path);
            Logger.Info($"Flame graph exported to {path}");
        });

        response = $"Flame graph will be exported in {delay}s to: {path}";
        return true;
    }
}