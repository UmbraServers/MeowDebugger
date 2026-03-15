using System;
using System.Reflection;
using HarmonyLib;
using LabApi.Events.Handlers;
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

    private static bool isDisabled = false;
    private static bool isLoaded = false;
    
    internal static void EnableTool()
    {
        isDisabled = false;
        Harmony ??= new Harmony($"MeowDebugger_{DateTime.Now}");
        _patcher ??= new Patcher(Harmony);

        if (ConfigDebugger.Instance!.ShouldPatchOnWaitingForPlayers)
            ServerEvents.WaitingForPlayers += OnLoadingPatch;
        else
            _patcher.PatchMethods();
    }

    internal static void DisableTool()
    {
        isDisabled = true;
        Harmony?.UnpatchAll(Harmony.Id);
    }

    internal static void OnLoadingPatch()
    {
        if (isLoaded || isDisabled || _patcher == null)
            return;
        
        _patcher.PatchMethods();
        isLoaded = true;
    }
}