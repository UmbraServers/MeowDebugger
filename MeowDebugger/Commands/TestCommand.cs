using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using CommandSystem;
using MeowDebugger.API.Features;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MeowDebugger.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class TestCommand : ICommand
{
    public string Command => "test";

    public string[] Aliases => [];

    public string Description => "Reports the slowest methods";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        AdminToyBase[] bases = Object.FindObjectsByType<AdminToyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        response = string.Join(", ", bases.Select(x => x.name));
        return true;
    }
}