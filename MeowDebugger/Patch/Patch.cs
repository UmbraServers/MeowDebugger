using System;
using System.Diagnostics;
using System.Reflection;
using MeowDebugger.API.Features;

namespace MeowDebugger.Patch;

internal class Patch
{
    private static void Prefix(MethodBase __originalMethod, out long __state)
    {
        MethodMetrics.Enter(__originalMethod);
        __state = Stopwatch.GetTimestamp();
    }

    private static void Postfix(MethodBase __originalMethod, long __state)
    {
        long elapsed = Stopwatch.GetTimestamp() - __state;
        MethodMetrics.Exit(__originalMethod, elapsed);
    }

    private static Exception Finalizer(MethodBase __originalMethod, long __state, Exception __exception)
    {
        long elapsed = Stopwatch.GetTimestamp() - __state;
        MethodMetrics.Exit(__originalMethod, elapsed);
        return __exception; // rethrow unchanged
    }
}