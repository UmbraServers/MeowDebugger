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
    // TODO: Make Snapshot receive MethodBase
    private static ConcurrentDictionary<MethodBase, Stats> Map { get; } = new();
    private static ConcurrentDictionary<MethodBase, ConcurrentDictionary<MethodBase, Stats>> ChildMap { get; } = new();

    [ThreadStatic]
    public readonly static Stack<(MethodBase Method, long ChildTicks, double BeforeTps)> StackValue = new();

    [ThreadStatic]
    public static List<FrameEvent>? FrameEvents;

    public static Dictionary<MethodBase, int> MethodIndexes { get; } = [];
    public static List<Frame> Frames { get; } = [];

    public static void Enter(MethodBase? method, long startTime)
    {
        if (method == null)
        {
            return;
        }

        StackValue.Push((method, 0, GetClampedTps()));
    }

    public static void Exit(MethodBase? method, long startTime, long endTime)
    {
        if (method == null)
        {
            return;
        }

        long elapsedTime = endTime - startTime;
        double tps = GetClampedTps();

        if (TicksToNanoSeconds(elapsedTime) >= ConfigDebugger.Instance!.NanosecondsThreshold)
        {
            List<FrameEvent> events = FrameEvents ??= [];

            int index = StoreIndex(method);

            events.Add(new FrameEvent(FrameEventType.OpenFrame, index, TicksToNanoSeconds(startTime)));
            events.Add(new FrameEvent(FrameEventType.CloseFrame, index, TicksToNanoSeconds(endTime)));
        }

        // Please I beg someone to make this code better, I didn't make this only changed to AddOrUpdate cause I like it instead of GetOrAdd
        if (StackValue.Count == 0)
        {
            Map.AddOrUpdate(method, _ => new Stats(), (m, stat) =>
            {
                stat.Add(elapsedTime, tps, tps);
                return stat;
            });
            return;
        }

        (MethodBase Method, long ChildTicks, double BeforeTps) = StackValue.Peek();

        if (!ReferenceEquals(Method, method))
        {
            Map.AddOrUpdate(method, _ => new Stats(), (m, stat) =>
            {
                stat.Add(elapsedTime, tps, tps);
                return stat;
            });
            return;
        }

        StackValue.Pop();

        long exclusiveTicks = Math.Max(0, elapsedTime - ChildTicks);

        Map.AddOrUpdate(method, _ => new Stats(), (m, stat) =>
        {
            stat.Add(exclusiveTicks, BeforeTps, tps);
            return stat;
        });

        if (StackValue.Count > 0)
        {
            var parent = StackValue.Pop();
            parent.ChildTicks += elapsedTime;
            StackValue.Push(parent);
            return;
        }
    }


    public static double GetClampedTps() => Mathf.Clamp((float)Server.Tps, 0, Server.MaxTps);

    public static string ReportAndReset(int topN = 10)
    {
        (MethodBase Method, Stats.Snapshot Snap)[] items = Map
            .Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
            .Where(x => x.Snap.Count > 0)
            .OrderByDescending(x => x.Snap.TotalTicks)
            .Take(topN)
            .ToArray();

        return BuildReport(items, includeChildren: false);
    }

    public static string ReportAndReset(IEnumerable<string> methodNames)
    {
        HashSet<string> set = new(methodNames, StringComparer.OrdinalIgnoreCase);

        (MethodBase Method, Stats.Snapshot Snap)[] items = Map
            .Where(kv => set.Contains(GetMethodName(kv.Key)))
            .Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
            .Where(x => x.Snap.Count > 0)
            .OrderByDescending(x => x.Snap.TotalTicks)
            .ToArray();

        return BuildReport(items, includeChildren: true);
    }

    private static double TicksToNanoSeconds(long ticks) => ticks * (Math.Pow(10, 9) / Stopwatch.Frequency);
    private static double TicksToMilliseconds(long ticks) => ticks * (Math.Pow(10, 3) / Stopwatch.Frequency);
    private static string GetMethodName(MethodBase method) => method.DeclaringType != null ? $"{method.DeclaringType.FullName}.{method.Name}" : method.Name;

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

    private static string BuildReport((MethodBase Method, Stats.Snapshot Snap)[] items, bool includeChildren)
    {
        if (items.Length == 0)
            return "No metrics collected yet.";

        StringBuilder sb = StringBuilderPool.Shared.Rent();
        sb.AppendLine("<#CF9F95>============================== Method Timing ===============================</color>");
        sb.AppendLine("<#7BB8DB>Avg(ms)\tMin(ms)\tMax(ms)\tCount\tTotal(ms)\tTPS Bfr\tTPS Aft\tDanger\t</color>");

        long maxTotal = items.Max(it => it.Snap.TotalTicks);

        foreach ((MethodBase Method, Stats.Snapshot Snap) in items)
        {
            sb.Append(MethodStats(Method, Snap, maxTotal));

            if (!ChildMap.TryRemove(Method, out var childMap))
                continue;

            var childItems = childMap
                .Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
                .Where(x => x.Snap.Count > 0)
                .OrderByDescending(x => x.Snap.TotalTicks)
                .Take(5)
                .ToArray();

            if (!includeChildren && childItems.Length == 0)
                continue;

            long childMaxTick = childItems.Max(c => c.Snap.TotalTicks);

            sb.AppendLine("  --- Inner Methods ---");
            
            foreach ((MethodBase childMethod, Stats.Snapshot childSnap) in childItems)
                sb.Append(MethodStats(childMethod, childSnap, childMaxTick));
        }
        sb.AppendLine("<#CF9F95>============================== Method Timing ===============================</color>");

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static string MethodStats(MethodBase method, Stats.Snapshot snap, long maxTotal)
    {
        int danger = 0;

        if (maxTotal > 0)
        {
            danger = (int)Math.Ceiling((double)snap.TotalTicks / maxTotal * 10);
            danger = Math.Max(1, Math.Min(10, danger));
        }

        string dangerHex = DangerToColorHex(danger);
        string methodName = GetMethodName(method);

        double avg = TicksToMilliseconds(snap.AvgTicks);
        double min = TicksToMilliseconds(snap.MinTicks);
        double max = TicksToMilliseconds(snap.MaxTicks);
        double total = TicksToMilliseconds(snap.TotalTicks);

        return $"<#F7FAB4>{methodName}</color>\n{(avg < 0.001 ? "<#96FFD1><0.001</color>" : $"{avg:0.###}")}\t{(min < 0.001 ? "<#96FFD1><0.001</color>" : $"{min:0.###}")}\t{max:0.###}\t{snap.Count}\t{total:0.###}\t{snap.BeforeTpsAvg:0.###}\t{snap.AfterTpsAvg:0.###}\t<#{dangerHex}>{danger}</color>\t\n";
    }

    private static string DangerToColorHex(int danger)
    {
        double t = (danger - 1) / 9.0;
        int r = (int)(t * 255);
        int g = (int)((1 - t) * 255);

        return $"{r:X2}{g:X2}00";
    }

    public sealed class Stats
    {
        private long _total;
        private int _count;
        private long _min = long.MaxValue;
        private long _max = 0;
        private double _beforeTpsTotal;
        private double _afterTpsTotal;
        private readonly object _gate = new();

        public void Add(long ticks, double beforeTps, double afterTps)
        {
            Interlocked.Add(ref _total, ticks);
            Interlocked.Increment(ref _count);

            lock (_gate)
            {
                _min = ticks < _min ? ticks : _min;
                _max = ticks > _max ? ticks : _max;
                _beforeTpsTotal += beforeTps;
                _afterTpsTotal += afterTps;
            }
        }

        public Snapshot SnapshotAndReset()
        {
            long total = Interlocked.Exchange(ref _total, 0);
            int count = Interlocked.Exchange(ref _count, 0);
            long min, max;
            double beforeTotal, afterTotal;

            lock (_gate)
            {
                min = _min; max = _max;
                _min = long.MaxValue; _max = 0;
                beforeTotal = _beforeTpsTotal; _beforeTpsTotal = 0;
                afterTotal = _afterTpsTotal; _afterTpsTotal = 0;
            }

            long avg = count > 0 ? total / Math.Max(1, count) : 0;
            double beforeAvg = count > 0 ? beforeTotal / Math.Max(1, count) : 0;
            double afterAvg = count > 0 ? afterTotal / Math.Max(1, count) : 0;

            if (count == 0) 
            {
                min = 0;
                max = 0; 
            }

            return new Snapshot(total, count, min, max, avg, beforeAvg, afterAvg);
        }

        public record Snapshot(
            long TotalTicks,
            int Count,
            long MinTicks,
            long MaxTicks,
            long AvgTicks,
            double BeforeTpsAvg,
            double AfterTpsAvg
        );
    }
}