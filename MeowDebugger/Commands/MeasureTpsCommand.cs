using CommandSystem;
using LabApi.Features.Console;
using MEC;
using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace MeowDebugger.Commands;

/// <summary>
/// Command for measuring tps in a window of time.
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class MeasureTpsCommand : ICommand
{
    /// <inheritdoc/>
    public string Command => "measuretps";

    /// <inheritdoc/>
    public string[] Aliases => [];

    /// <inheritdoc/>
    public string Description => "Generates a flamegraph.";

    /// <inheritdoc/>
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count == 0 || !int.TryParse(arguments.At(0), out int seconds))
        {
            response = "You need to pass a valid number.";
            return false;
        }

        Timing.RunCoroutine(Measurer(seconds));

        response = $"Measuring tps for {seconds} look at server console";
        return true;
    }

    private static IEnumerator<float> Measurer(int seconds)
    {
        double totalTps = 0;
        int samples = 0;

        while (samples < seconds)
        {
            totalTps += Server.Tps;
            samples++;

            yield return Timing.WaitForSeconds(1f);
        }

        Logger.Info($"Average tps: {totalTps / samples}");
    }
}