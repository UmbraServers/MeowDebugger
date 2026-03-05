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
    public override string Name => "MeowDebugger";

    /// <inheritdoc/>
    public override string Description => "Debugger for SCP:SL";

    /// <inheritdoc/>
    public override string Author => "@notzer0two";

    /// <inheritdoc/>
    public override LoadPriority Priority => LoadPriority.Lowest;

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
    
    internal static MeowDebugger? Instance { get; private set; }

    private Patcher? _patcher;

    private bool _enabled;

    
    /// <inheritdoc/>
    public override void Enable()
    {
        Instance = this;
        EnableTool();
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        DisableTool();
    }
    
    internal void EnableTool()
    {
        if (_enabled)
            return;

        Harmony ??= new Harmony("MeowDebugger_" + DateTime.Now);
        _patcher ??= new Patcher(Harmony);
        _patcher.PatchMethods();
        _enabled = true;
    }

    internal void DisableTool()
    {
        if (!_enabled)
            return;

        Harmony?.UnpatchAll(Harmony.Id);
        _enabled = false;
    }
}