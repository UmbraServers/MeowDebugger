using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;

namespace MeowDebugger.Commands.SubCommand
{
#if !EXILED_RELEASE
    [CommandHandler(typeof(ReporterParentCommand))]
#endif
    public class TpsCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "tps";

        /// <inheritdoc/>
        public string[] Aliases => [];

        /// <inheritdoc/>
        public string Description => "Measures the tps within a time frame.";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0 || !int.TryParse(arguments.At(0), out int seconds))
            {
                response = "You need to pass a valid number.";
                return false;
            }

            Timing.RunCoroutine(Measurer(seconds));

            response = $"Measuring tps for {seconds}s look at server console";
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
}
