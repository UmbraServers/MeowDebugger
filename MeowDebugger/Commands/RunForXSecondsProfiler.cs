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
            response = "You need to pass the ammount of time.";
            return true;
        }

        string first = arguments.Array[arguments.Offset]!.ToLowerInvariant();
        string seconds = arguments.Array[arguments.Offset + 1]!.ToLowerInvariant();

        if (!int.TryParse(first, out int result))
        {
            response = "Please pass a valid number.";
            return false;
        }

        int bots = int.TryParse(seconds, out int r) ? r : 1;

        for (int i = 0; i < bots; i++)
        {
            DummyUtils.SpawnDummy();
        }
        
        string path = Path.Combine(PathManager.Configs.FullName, "flame-generated.txt");
        
        MEC.Timing.CallDelayed(result, () =>
        {
            MethodMetrics.ExportFlameGraph(path);
            Logger.Info($"Flame graph exported to {path}");
        });

        response = "wait for " + result + $" seconds, then check the file {path} for the flame graph.";
        return true;
    }
}