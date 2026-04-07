using System;
using System.Diagnostics;
using System.Reflection;
using MeowDebugger.API.Features;

namespace MeowDebugger.Patch;

internal class Patch
{
    private static void Prefix(MethodBase __originalMethod, out long __state)
    {
        __state = Stopwatch.GetTimestamp();
    }

    private static Exception Finalizer(MethodBase __originalMethod, long __state, Exception __exception)
    {
        long elapsed = Stopwatch.GetTimestamp();
        MethodMetrics.Exit(__originalMethod, __state, elapsed);
        return __exception;
    }
}