#if EXILED_RELEASE
using Exiled.API.Interfaces;
#endif

using System.Collections.Generic;
using System.ComponentModel;

namespace MeowDebugger;

/// <summary>
/// Represents the class for the Debugger Configs.
/// </summary>
public class ConfigDebugger
#if EXILED_RELEASE
        : IConfig
#endif
{
    /// <summary>
    /// Represents the config instance.
    /// </summary>
    public static ConfigDebugger? Instance { get; internal set; }

#if EXILED_RELEASE
    public bool IsEnabled { get; set; } = true;

    public bool Debug { get; set; }

    public bool ShouldPatchLabApiPlugins { get; set; } = true;
#endif        

    [Description("It prevents the following dlls to be debugged")]
    public List<string> BlacklistAssemblies { get; set; } =
    [
        "CedModV3",
        "0Harmony",
        "NVorbis",
        "Mono.Posix",
        "SemanticVersioning",
        "System.Buffers",
        "System.ComponentModel.DataAnnotations",
        "System.Memory",
        "System.Numerics.Vectors",
        "System.Runtime.CompilerServices.Unsafe",
        "System.ValueTuple"
    ];

    public List<string> WhitelistNamespaces { get; set; } =
    [
        "InventorySystem",
        "CommandSystem"
    ];

    [Description("Minimal nanoseconds for the speedscope file / 1ms = 1000000ns / (I don't recommend setting this to 0) ")]
    public double NanosecondsThreshold { get; set; } = 200000;

    [Description("The output of the profiler")]
    public string SpeedscopeOutputPath { get; set; } = string.Empty;

    [Description("If it the output file will contain the filtered namespace name")]
    public bool ShouldIncludeNamespaceInOutput { get; set; } = true;

    [Description("If it should patch on loading for players rather than on the boot of the plugin")]
    public bool ShouldPatchOnWaitingForPlayers { get; set; } = true;
}