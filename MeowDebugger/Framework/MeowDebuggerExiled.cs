#if EXILED_RELEASE
using System;
using System.Reflection;
using Exiled.API.Features;
using HarmonyLib;
using MeowDebugger.API;
using MeowDebugger.API.Features;

namespace MeowDebugger.Framework;

public class MeowDebuggerExiled  : Plugin<ConfigDebugger>
{
    public override string Name { get; } = BuildSetting.PluginName;
    public override string Author { get; } = BuildSetting.Author;
    public override Version Version { get; } = GeneralUtils.Assembly.GetName().Version;
    
    /// <summary>
    /// Gets the harmony to use for the API.
    /// </summary>
    internal static Harmony? Harmony { get; private set; }
    
    internal static MeowDebuggerExiled? Instance { get; private set; }

    private Patcher? _patcher;
    
    public override void OnEnabled()
    {
        Instance = this;
        ConfigDebugger.Instance = Config;
        GeneralUtils.EnableTool();
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        ConfigDebugger.Instance = null;
        GeneralUtils.DisableTool();
    }
}
#endif  