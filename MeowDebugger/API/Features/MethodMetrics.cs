using LabApi.Features.Wrappers;
using MeowDebugger.API.Features.Speedscope.File.Structs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using TMPro;
using UnityEngine;

namespace MeowDebugger.API.Features;

internal static class MethodMetrics
{
    private static ConcurrentDictionary<MethodBase, Stats> _map = new();
    private static ConcurrentDictionary<MethodBase, ConcurrentDictionary<MethodBase, Stats>> _children = new();

    [ThreadStatic]
    public readonly static Stack<(MethodBase Method, long ChildTicks, double BeforeTps)> StackValue = new();

    [ThreadStatic]
    public static List<FrameEvent>? Events;

    public static Dictionary<MethodBase, int> MethodIndexes { get; } = [];
    public static List<Frame> Frames { get; } = [];


    private static readonly double TicksToNano = Math.Pow(10, 9) / Stopwatch.Frequency;
    private static double TicksToNanoSeconds(long ticks) => ticks * TicksToNano;

    public static void Enter(MethodBase? method, long startTime)
    {
        if (method == null)
        {
            return;
        }

        StackValue.Push((method, 0, GetClampedTps()));
    }

    public static void Exit(MethodBase? method, long endTime, long startTime)
    {
        if (method == null)
        {
            return;
        }

        long elapsedTime = endTime - startTime;

        if (TicksToNanoSeconds(elapsedTime) >= ConfigDebugger.Instance!.NanosecondsThreshold)
        {
            List<FrameEvent> events = Events ??= new();

            int index = StoreIndex(method);

            if (index == -1)
                return;

            // this is so much better dude, I don't have to bother filtering it at the end + less memory usage!!!!!!
            events.Add(new FrameEvent(EventType.OpenFrame, index, TicksToNanoSeconds(startTime)));
            events.Add(new FrameEvent(EventType.CloseFrame, index, TicksToNanoSeconds(endTime)));
        }

        if (StackValue.Count == 0)
        {
            _map.AddOrUpdate(method, _ => new Stats(), (m, stat) =>
            {
                double tps = GetClampedTps();
                stat.Add(elapsedTime, tps, tps);
                return stat;
            });
            return;
        }

        (MethodBase Method, long ChildTicks, double BeforeTps) = StackValue.Peek();

        if (!ReferenceEquals(Method, method))
        {
            _map.AddOrUpdate(method, _ => new Stats(), (m, stat) =>
            {
                double tps = GetClampedTps();
                stat.Add(elapsedTime, tps, tps);
                return stat;
            });
            return;
        }

        StackValue.Pop();

        long exclusiveTicks = elapsedTime - ChildTicks;

        if (exclusiveTicks < 0)
        {
            exclusiveTicks = 0;
        }

        double beforeTps = BeforeTps;
        double afterTps = GetClampedTps();

        _map.AddOrUpdate(method, _ => new Stats(), (m, stat) =>
        {
            stat.Add(exclusiveTicks, beforeTps, afterTps);
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

    public static int StoreIndex(MethodBase method)
    {
        if (MethodIndexes.TryGetValue(method, out int id))
            return id;

        id = Frames.Count;

        string methodName = method.DeclaringType != null ? $"{method.DeclaringType.FullName}.{method.Name}" : method.Name;

        Frame frame = new Frame(methodName, method.Module.FullyQualifiedName);

        Frames.Add(frame);

        MethodIndexes[method] = id;
        return id;
    }

    public static int GetMethodIndex(MethodBase method) => MethodIndexes.TryGetValue(method, out int id) ? id : -1;

    private static double GetClampedTps()
    {
        double tps = Server.Tps;
        if (tps > Server.MaxTps) tps = Server.MaxTps;
        else if (tps < 0) tps = 0;
        return tps;
    }

    public static string ReportAndReset(int topN = 10)
    {
        (MethodBase Method, Stats.Snapshot Snap)[] items = _map.Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
                        .Where(x => x.Snap.Count > 0)
                        .OrderByDescending(x => x.Snap.TotalTicks)
                        .Take(topN)
                        .ToArray();

        return BuildReport(items, includeChildren: false);
    }

    public static (MethodBase Method, Stats.Snapshot Snap)[] SnapshotAllAndReset()
    {
        List<(MethodBase, Stats.Snapshot)> results = new List<(MethodBase, Stats.Snapshot)>();
        foreach (MethodBase? key in MethodMetrics._map.Keys.ToArray())
        {
            if (!MethodMetrics._map.TryGetValue(key, out Stats? stats))
                continue;

            Stats.Snapshot snap = stats.SnapshotAndReset();
            if (snap.Count > 0)
                results.Add((key, snap));
        }
        return results.ToArray();
    }

    public static string ReportAndReset(IEnumerable<string> methodNames)
    {
        var set = new HashSet<string>(methodNames, StringComparer.OrdinalIgnoreCase);
        var items = _map
            .Where(kv =>
            {
                var name = kv.Key.Name;
                var full = kv.Key.DeclaringType != null
                    ? $"{kv.Key.DeclaringType.FullName}.{name}"
                    : name;
                return set.Contains(name) || set.Contains(full);
            })
            .Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
            .Where(x => x.Snap.Count > 0)
            .OrderByDescending(x => x.Snap.TotalTicks)
            .ToArray();

        return BuildReport(items, includeChildren: true);
    }

    private static string BuildReport((MethodBase Method, Stats.Snapshot Snap)[] items, bool includeChildren)
    {
        if (items.Length == 0)
        {
            return "No metrics collected yet.";
        }

        double ToMs(long t) => t * 1000.0 / Stopwatch.Frequency;

        var sb = new StringBuilder();
        sb.AppendLine("==== Method Timing (last window) ====");
        sb.AppendLine("Avg(ms)\tMin(ms)\tMax(ms)\tCount\tTotal(ms)\tTPS Before\tTPS After\tDanger\tMethod");
        long maxTotal = items.Max(it => it.Snap.TotalTicks);
        foreach (var it in items)
        {
            int danger = 0;
            if (maxTotal > 0)
            {
                danger = (int)Math.Ceiling((double)it.Snap.TotalTicks / maxTotal * 10);
                danger = Math.Max(1, Math.Min(10, danger));
            }
            string dangerHex = DangerToColorHex(danger);
            sb.AppendFormat("{0:0.###}\t{1:0.###}\t{2:0.###}\t{3}\t{4:0.###}\t{5:0.###}\t{6:0.###}\t{7}\t{8}\n",
                ToMs(it.Snap.AvgTicks),
                ToMs(it.Snap.MinTicks),
                ToMs(it.Snap.MaxTicks),
                it.Snap.Count,
                ToMs(it.Snap.TotalTicks),
                it.Snap.BeforeTpsAvg,
                it.Snap.AfterTpsAvg,
                $"<color=#{dangerHex}>{danger}</color>",
                it.Method.DeclaringType != null
                    ? $"{it.Method.DeclaringType.FullName}.{it.Method.Name}"
                    : it.Method.Name);

            if (_children.TryRemove(it.Method, out var childMap))
            {
                var childItems = childMap
                    .Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
                    .Where(x => x.Snap.Count > 0)
                    .OrderByDescending(x => x.Snap.TotalTicks)
                    .Take(5)
                    .ToArray();

                if (includeChildren && childItems.Length > 0)
                {
                    sb.AppendLine("  --- Inner Methods ---");
                    long maxChild = childItems.Max(c => c.Snap.TotalTicks);
                    foreach (var child in childItems)
                    {
                        int cd = 0;
                        if (maxChild > 0)
                        {
                            cd = (int)Math.Ceiling((double)child.Snap.TotalTicks / maxChild * 10);
                            cd = Math.Max(1, Math.Min(10, cd));
                        }
                        string cHex = DangerToColorHex(cd);
                        sb.AppendFormat("    {0:0.###}\t{1:0.###}\t{2:0.###}\t{3}\t{4:0.###}\t{5:0.###}\t{6:0.###}\t{7}\t{8}\n",
                            ToMs(child.Snap.AvgTicks),
                            ToMs(child.Snap.MinTicks),
                            ToMs(child.Snap.MaxTicks),
                            child.Snap.Count,
                            ToMs(child.Snap.TotalTicks),
                            child.Snap.BeforeTpsAvg,
                            child.Snap.AfterTpsAvg,
                            $"<color=#{cHex}>{cd}</color>",
                            child.Method.DeclaringType != null
                                ? $"{child.Method.DeclaringType.FullName}.{child.Method.Name}"
                                : child.Method.Name);
                    }
                }
            }
        }
        return sb.ToString();
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
                if (ticks < _min) _min = ticks;
                if (ticks > _max) _max = ticks;
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
            if (count == 0) { min = 0; max = 0; }
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