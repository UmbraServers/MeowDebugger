using LabApi.Loader;
using MeowDebugger.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MeowDebugger.API.Features.Speedscope;

/// <summary>
/// Provides functionality to export profiling data to a JSON file in a format compatible with Flamescope.
/// </summary>
public class ExportToFlamescope
{
    /// <summary>
    /// Exports the json file.
    /// </summary>
    /// <returns>the json file</returns>
    public static bool ExportJsonFile()
    {
    }

    // 
    private static readonly double TicksToNano = 10^9 / Stopwatch.Frequency;

    private static double TicksToNanoSeconds(long ticks) => ticks * TicksToNano;
}