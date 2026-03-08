using System.Collections.Generic;
using System.ComponentModel;

#if EXILED_RELEASE
using Exiled.API.Interfaces;
#endif

namespace MeowDebugger;

/// <summary>
/// Debugger Configs.
/// </summary>
public class ConfigDebugger
#if EXILED_RELEASE
        : IConfig
#endif
{
    public static ConfigDebugger? Instance { get; internal set; }
    
#if EXILED_RELEASE
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
#endif
    
    /// <summary>
    /// Gets or sets the list of Blacklisted DLLs.
    /// </summary>
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
}