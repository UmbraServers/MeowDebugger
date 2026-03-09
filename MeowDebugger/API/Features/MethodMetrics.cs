using System;
using System.IO;
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
    private readonly static ConcurrentDictionary<MethodBase, Stats> _map = new();
    private readonly static ConcurrentDictionary<MethodBase, ConcurrentDictionary<MethodBase, Stats>> _children = new();
    private readonly static ConcurrentDictionary<string, long> _flame = new();
    
    [ThreadStatic]
    private static Stack<(MethodBase Method, long ChildTicks, double BeforeTps)>? _stackValue;

    public static void Enter(MethodBase? method)
    {
        if (method == null) return;

        var stack = _stackValue ??= new Stack<(MethodBase, long, double)>();
        stack.Push((method, 0, GetClampedTps()));
    }

    public static void Exit(MethodBase? method, long elapsedTicks)
    {
        if (method == null) return;
        
        Stack<(MethodBase Method, long ChildTicks, double BeforeTps)> stack = MethodMetrics._stackValue ??= new Stack<(MethodBase, long, double)>();
        
        if (stack.Count == 0) return;

        (MethodBase Method, long ChildTicks, double BeforeTps) popped = stack.Pop();
        if (!ReferenceEquals(popped.Method, method)) return;

        long exclusiveTicks = elapsedTicks - popped.ChildTicks;
        if (exclusiveTicks < 0) exclusiveTicks = 0;

        List<string> frames = stack
            .Reverse()
            .Select(s => FormatMethodName(s.Method))
            .ToList();

        frames.Add(FormatMethodName(method));
        string key = string.Join(";", frames);

        if (MethodMetrics._flame.Count < 100_000) 
        {
            MethodMetrics._flame.AddOrUpdate(key, exclusiveTicks, (_, old) => old + exclusiveTicks);
        }

        double beforeTps = popped.BeforeTps;
        double afterTps = GetClampedTps();

        var s = _map.GetOrAdd(method, _ => new Stats());

        s.Add(exclusiveTicks, beforeTps, afterTps);
        if (stack.Count > 0)
        {
            var parent = stack.Pop();
            stack.Push((parent.Method, parent.ChildTicks + elapsedTicks, parent.BeforeTps));
        }
    }

    private static string FormatMethodName(MethodBase m)
    {
        return m.DeclaringType != null
            ? $"{m.DeclaringType.FullName}.{m.Name}"
            : m.Name;
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
        (MethodBase Method, Stats.Snapshot Snap)[] items = _map.Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
                        .Where(x => x.Snap.Count > 0)
                        .OrderByDescending(x => x.Snap.TotalTicks)
                        .Take(topN)
                        .ToArray();

        return BuildReport(items, includeChildren: false);
    }

    public static void ExportFlameGraph(string path)
    {
        var snapshot = _flame.ToArray();

        double ticksPerUs = Stopwatch.Frequency / 1_000_000.0;

        using StreamWriter sw = new StreamWriter(path, append: false, Encoding.UTF8);
        sw.NewLine = "\n";

        foreach (var kv in snapshot)
        {
            if (kv.Value <= 0)
            {
                continue;
            }

            long us = (long)(kv.Value / ticksPerUs);

            if (us <= 0)
            {
                us = 1;
            }  

            sw.WriteLine($"{kv.Key} {us}");
        }

        _flame.Clear();
    }

    private static (MethodBase Method, Stats.Snapshot Snap)[] SnapshotAllAndReset()
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

    public static void ExportCsv(string path)
    {
        var items = SnapshotAllAndReset();

        double ToMs(long t) => t * 1000.0 / Stopwatch.Frequency;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Method,Count,TotalMs,AvgMs,MinMs,MaxMs,TpsBefore,TpsAfter,TpsDrop");

        foreach (var it in items)
        {
            var s = it.Snap;

            string name = it.Method.DeclaringType != null
                ? $"{it.Method.DeclaringType.FullName}.{it.Method.Name}"
                : it.Method.Name;

            double totalMs = ToMs(s.TotalTicks);
            double avgMs = ToMs(s.AvgTicks);
            double minMs = ToMs(s.MinTicks);
            double maxMs = ToMs(s.MaxTicks);
            double tpsDrop = s.BeforeTpsAvg - s.AfterTpsAvg;

            sb.AppendLine($"{name}," +
                          $"{s.Count}," +
                          $"{totalMs:0.###}," +
                          $"{avgMs:0.###}," +
                          $"{minMs:0.###}," +
                          $"{maxMs:0.###}," +
                          $"{s.BeforeTpsAvg:0.###}," +
                          $"{s.AfterTpsAvg:0.###}," +
                          $"{tpsDrop:0.###}");
        }

        File.WriteAllText(path, sb.ToString());
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

    public static void ExportHeatmapHtml(string path)
    {

        var items = SnapshotAllAndReset();

        if (items.Length == 0)
        {
            return;
        }

        double ToMs(long t) => t * 1000.0 / Stopwatch.Frequency;

        double maxTotal = items.Max(i => ToMs(i.Snap.TotalTicks));

        var sb = new StringBuilder();
        sb.AppendLine("<html><body><table border='1' style='border-collapse:collapse;'>");
        sb.AppendLine("<tr><th>Method</th><th>Total(ms)</th><th>Avg(ms)</th><th>TPS Drop</th></tr>");

        foreach (var tuplethingy in items)
        {
            var s = tuplethingy.Snap;

            double totalMs = ToMs(s.TotalTicks);
            double avgMs = ToMs(s.AvgTicks);
            double tpsDrop = s.BeforeTpsAvg - s.AfterTpsAvg;

            double ratio = maxTotal > 0 ? totalMs / maxTotal : 0;

            int r = (int)(ratio * 255);
            int g = (int)((1 - ratio) * 255);

            string color = $"#{r:X2}{g:X2}00";

            string name = tuplethingy.Method.DeclaringType != null
                ? $"{tuplethingy.Method.DeclaringType.FullName}.{tuplethingy.Method.Name}"
                : tuplethingy.Method.Name;

            sb.AppendLine(
                $"<tr style='background-color:{color}'>" +
                $"<td>{name}</td>" +
                $"<td>{totalMs:0.###}</td>" +
                $"<td>{avgMs:0.###}</td>" +
                $"<td>{tpsDrop:0.###}</td>" +
                $"</tr>");
        }

        sb.AppendLine("</table></body></html>");

        File.WriteAllText(path, sb.ToString());
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
        
        public readonly struct Snapshot(
            MethodBase Method,
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