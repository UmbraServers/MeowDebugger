#if !EXILED_RELEASE
using System;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using MeowDebugger.API;

namespace MeowDebugger.Framework;

/// <summary>
/// Represents the class for the LabAPI version of MeowDebugger.
/// </summary>
public class MeowDebuggerLabAPI : Plugin<ConfigDebugger>
{
    /// <inheritdoc/>
    public override string Name => BuildSetting.PluginName;

    /// <inheritdoc/>
    public override string Description => BuildSetting.PluginDescription;

    /// <inheritdoc/>
    public override string Author => BuildSetting.Author;

    /// <inheritdoc/>
    public override LoadPriority Priority => LoadPriority.Lowest;

    /// <inheritdoc/>
    public override Version Version { get; } = GeneralUtils.Assembly.GetName().Version;

    /// <inheritdoc/>
    public override Version RequiredApiVersion { get; } = new(LabApi.Features.LabApiProperties.CompiledVersion);
    
    internal static MeowDebuggerLabAPI? Instance { get; private set; }

    
    /// <inheritdoc/>
    public override void Enable()
    {
        Instance = this;
        ConfigDebugger.Instance = Config;
        GeneralUtils.EnableTool();
    }

    /// <inheritdoc/>
    public override void Disable()
    {
        ConfigDebugger.Instance = null;
        GeneralUtils.DisableTool();
    }
}
#endif