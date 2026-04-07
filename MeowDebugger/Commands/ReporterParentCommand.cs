using CommandSystem;
using LabApi.Features.Wrappers;
using MeowDebugger.API.Features;
using MeowDebugger.Commands.SubCommand;
using System;

namespace MeowDebugger.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class ReporterParentCommand : ParentCommand
    {
        public ReporterParentCommand() => this.LoadGeneratedCommands();
        public override string Command => "reporter";

        public override string[] Aliases => [];

        public override string Description => "Reports the slowest methods";

        public override void LoadGeneratedCommands()
        {
#if EXILED_RELEASE
            this.RegisterCommand(new DisableCommand());
            this.RegisterCommand(new EnableCommand());
            this.RegisterCommand(new FilterCommand());
            this.RegisterCommand(new FlameCommand());
            this.RegisterCommand(new HelpCommand());
            this.RegisterCommand(new TopCommand());
            this.RegisterCommand(new TpsCommand());
#endif
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = $"Unknown subcommand. Use 'reporter help' for usage.";
            return false;
        }
    }
}
