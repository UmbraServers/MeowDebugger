using LabApi.Features.Wrappers;
using MeowDebugger.API.Features.Speedscope.File.Structs;
using NorthwoodLib.Pools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MeowDebugger.API.Features;

internal static class MethodMetrics
{
    public static ThreadLocal<ThreadEvents> FrameEvents = new(() => new ThreadEvents()
    {
        ThreadId = Thread.CurrentThread.ManagedThreadId,
    });
    
    public static Dictionary<MethodBase, int> MethodIndexes { get; } = [];
    
    public static List<Frame> Frames { get; } = [];
    
    public static void Exit(MethodBase? method, long startTime, long endTime)
    {
        if (method == null)
        {
            return;
        }

        long elapsedTime = endTime - startTime;

        if (TicksToNanoSeconds(elapsedTime) < ConfigDebugger.Instance!.NanosecondsThreshold)
        {
            return;
        }
        
        List<FrameEvent> events = FrameEvents.Value.Events;

        int index = StoreIndex(method);

        events.Add(new FrameEvent(FrameEventType.OpenFrame, index, TicksToNanoSeconds(startTime)));
        events.Add(new FrameEvent(FrameEventType.CloseFrame, index, TicksToNanoSeconds(endTime)));
    }
    
    public static double GetClampedTps() => Mathf.Clamp((float)Server.Tps, 0, Server.MaxTps);

    private static double TicksToNanoSeconds(long ticks) => ticks * (Math.Pow(10, 9) / Stopwatch.Frequency);

    private static int StoreIndex(MethodBase method)
    {
        if (MethodIndexes.TryGetValue(method, out int id))
            return id;

        id = Frames.Count;

        string methodName = method.DeclaringType != null ? $"{method.DeclaringType.FullName}.{method.Name}" : method.Name;
        Frame frame = new(methodName, method.Module.FullyQualifiedName);

        Frames.Add(frame);

        MethodIndexes[method] = id;
        return id;
    }

    public record ThreadEvents
    {
        public int ThreadId;
        public List<FrameEvent> Events = new();
    }
}