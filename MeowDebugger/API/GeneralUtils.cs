using System;
using System.Reflection;
using HarmonyLib;
using MeowDebugger.API.Features;

namespace MeowDebugger.API;

internal static class GeneralUtils
{
    /// <summary>
    /// Gets the Assembly of the API.
    /// </summary>
    internal static Assembly Assembly { get; } = typeof(Patcher).Assembly;
    
    /// <summary>
    /// Gets the harmony to use for the API.
    /// </summary>
    private static Harmony? Harmony { get; set; }
    
    private static Patcher? _patcher;
    
    internal static void EnableTool()
    {
        Harmony ??= new Harmony($"MeowDebugger_{DateTime.Now}");
        _patcher ??= new Patcher(Harmony);
        _patcher.PatchMethods();
    }

    internal static void DisableTool()
    {
        Harmony?.UnpatchAll(Harmony.Id);
    }
}