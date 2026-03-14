#if EXILED_RELEASE
using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MeowDebugger.API;

namespace MeowDebugger.Framework;

/// <summary>
/// Represents the class for the LabAPI version of MeowDebugger.
/// </summary>
public class MeowDebuggerExiled  : Plugin<ConfigDebugger>
{
    /// <inheritdoc/>
    public override string Name { get; } = BuildSetting.PluginName;
    
    /// <inheritdoc/>
    public override string Author { get; } = BuildSetting.Author;
    
    /// <inheritdoc/>
    public override Version Version { get; } = GeneralUtils.Assembly.GetName().Version;

    /// <inheritdoc/>
    public override PluginPriority Priority { get; } = PluginPriority.Lowest;

    /// <summary>
    /// Gets the harmony to use for the API.
    /// </summary>
    internal static Harmony? Harmony { get; private set; }
    
    internal static MeowDebuggerExiled? Instance { get; private set; }
    
    /// <inheritdoc/>
    public override void OnEnabled()
    {
        Instance = this;
        ConfigDebugger.Instance = Config;
        GeneralUtils.EnableTool();
        base.OnEnabled();
    }

    /// <inheritdoc/>
    public override void OnDisabled()
    {
        ConfigDebugger.Instance = null;
        GeneralUtils.DisableTool();
    }
}
#endif  