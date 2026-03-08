using System.Collections.Generic;
using System.ComponentModel;

namespace MeowDebugger;

/// <summary>
/// Debugger Configs.
/// </summary>
public sealed class ConfigDebugger
{
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