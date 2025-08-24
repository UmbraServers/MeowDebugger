using System;
using System.Reflection;
using HarmonyLib;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using MeowDebugger.API.Features;
using UnityEngine.SceneManagement;

namespace MeowDebugger;

public class MeowDebugger : Plugin
{
    /// <inheritdoc/>
    public override string Name => "SSBMain";

    /// <inheritdoc/>
    public override string Description => "Super Smash Bros SL: Edition";

    /// <inheritdoc/>
    public override string Author => "@notzer0two";

    /// <inheritdoc/>
    public override LoadPriority Priority => LoadPriority.Highest;

    /// <inheritdoc/>
    public override Version Version { get; } = Assembly.GetName().Version;

    /// <inheritdoc/>
    public override Version RequiredApiVersion { get; } = new(LabApi.Features.LabApiProperties.CompiledVersion);

    /// <summary>
    /// Gets the harmony to use for the API.
    /// </summary>
    internal static Harmony? Harmony { get; private set; }

    /// <summary>
    /// Gets the Assembly of the API.
    /// </summary>
    internal static Assembly Assembly { get; } = typeof(MeowDebugger).Assembly;
    
    private Patcher _patcher;
    
    /// <inheritdoc/>
    public override void Enable()
    {
        Harmony = new Harmony("MeowDebugger_" + DateTime.Now);
        _patcher = new(Harmony);
        _patcher.PatchMethods();
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        Harmony?.UnpatchAll(Harmony.Id);
    }
}