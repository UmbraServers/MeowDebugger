using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using LabApi.Features.Wrappers;

namespace MeowDebugger.API.Features;

internal static class MethodMetrics
{
    private static readonly ConcurrentDictionary<MethodBase, Stats> _map = new();
    private static readonly ConcurrentDictionary<MethodBase, ConcurrentDictionary<MethodBase, Stats>> _children = new();
    private static readonly AsyncLocal<Stack<(MethodBase Method, double BeforeTps)>> _stack = new();

    public static void Enter(MethodBase? method)
    {
        if (method == null) return;
        var stack = _stack.Value ??= new Stack<(MethodBase, double)>();
        stack.Push((method, GetClampedTps()));
    }

    public static void Exit(MethodBase? method, long elapsedTicks)
    {
        if (method == null) return;
        var stack = _stack.Value;
        if (stack == null || stack.Count == 0) return;
        var popped = stack.Pop();
        if (!ReferenceEquals(popped.Method, method)) return;

        double beforeTps = popped.BeforeTps;
        double afterTps = GetClampedTps();

        var s = _map.GetOrAdd(method, _ => new Stats());
        s.Add(elapsedTicks, beforeTps, afterTps);

        if (stack.Count > 0)
        {
            var parent = stack.Peek().Method;
            var map = _children.GetOrAdd(parent, _ => new ConcurrentDictionary<MethodBase, Stats>());
            var childStats = map.GetOrAdd(method, _ => new Stats());
            childStats.Add(elapsedTicks, beforeTps, afterTps);
        }
    }

    private static double GetClampedTps()
    {
        double tps = Server.Tps;
        if (tps > Server.MaxTps) tps = Server.MaxTps;
        else if (tps < 0) tps = 0;
        return tps;
    }

    public static string ReportAndReset(int topN = 10)
    {
        var items = _map.Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
                        .Where(x => x.Snap.Count > 0)
                        .OrderByDescending(x => x.Snap.TotalTicks)
                        .Take(topN)
                        .ToArray();

        return BuildReport(items, includeChildren: false);
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
        if (items.Length == 0) return null;

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
        double t = (danger - 1) / 9.0; // 0 for danger=1, 1 for danger=10
        int r = (int)(t * 255);
        int g = (int)((1 - t) * 255);
        return $"{r:X2}{g:X2}00";
    }

    private sealed class Stats
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

        public readonly struct Snapshot
        {
            public Snapshot(long totalTicks, int count, long minTicks, long maxTicks, long avgTicks, double beforeTpsAvg, double afterTpsAvg)
            { TotalTicks = totalTicks; Count = count; MinTicks = minTicks; MaxTicks = maxTicks; AvgTicks = avgTicks; BeforeTpsAvg = beforeTpsAvg; AfterTpsAvg = afterTpsAvg; }

            public long TotalTicks { get; }
            public int Count { get; }
            public long MinTicks { get; }
            public long MaxTicks { get; }
            public long AvgTicks { get; }
            public double BeforeTpsAvg { get; }
            public double AfterTpsAvg { get; }
        }
    }
}