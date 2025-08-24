using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MeowDebugger.API.Features;

internal static class MethodMetrics
{
     private static readonly ConcurrentDictionary<MethodBase, Stats> _map = new();

    public static void Record(MethodBase method, long elapsedTicks)
    {
        var s = _map.GetOrAdd(method, _ => new Stats());
        s.Add(elapsedTicks);
    }

    public static string ReportAndReset(int topN = 10)
    {
        var items = _map.Select(kv => (Method: kv.Key, Snap: kv.Value.SnapshotAndReset()))
                        .Where(x => x.Snap.Count > 0)
                        .OrderByDescending(x => x.Snap.TotalTicks)
                        .Take(topN)
                        .ToArray();

        if (items.Length == 0) return null;

        double ToMs(long t) => t * 1000.0 / Stopwatch.Frequency;

        var sb = new StringBuilder();
        sb.AppendLine("==== Method Timing (last window) ====");
        sb.AppendLine("Avg(ms)\tMin(ms)\tMax(ms)\tCount\tTotal(ms)\tMethod");
        foreach (var it in items)
        {
            sb.AppendFormat("{0:0.###}\t{1:0.###}\t{2:0.###}\t{3}\t{4:0.###}\t{5}\n",
                ToMs(it.Snap.AvgTicks),
                ToMs(it.Snap.MinTicks),
                ToMs(it.Snap.MaxTicks),
                it.Snap.Count,
                ToMs(it.Snap.TotalTicks),
                it.Method.DeclaringType != null
                    ? $"{it.Method.DeclaringType.FullName}.{it.Method.Name}"
                    : it.Method.Name);
        }
        return sb.ToString();
    }

    private sealed class Stats
    {
        private long _total;
        private int _count;
        private long _min = long.MaxValue;
        private long _max = 0;
        private readonly object _gate = new();

        public void Add(long ticks)
        {
            Interlocked.Add(ref _total, ticks);
            Interlocked.Increment(ref _count);
            lock (_gate)
            {
                if (ticks < _min) _min = ticks;
                if (ticks > _max) _max = ticks;
            }
        }

        public Snapshot SnapshotAndReset()
        {
            long total = Interlocked.Exchange(ref _total, 0);
            int count = Interlocked.Exchange(ref _count, 0);
            long min, max;
            lock (_gate)
            {
                min = _min; max = _max;
                _min = long.MaxValue; _max = 0;
            }
            long avg = count > 0 ? total / Math.Max(1, count) : 0;
            if (count == 0) { min = 0; max = 0; }
            return new Snapshot(total, count, min, max, avg);
        }

        public readonly struct Snapshot
        {
            public Snapshot(long totalTicks, int count, long minTicks, long maxTicks, long avgTicks)
            { TotalTicks = totalTicks; Count = count; MinTicks = minTicks; MaxTicks = maxTicks; AvgTicks = avgTicks; }

            public long TotalTicks { get; }
            public int Count { get; }
            public long MinTicks { get; }
            public long MaxTicks { get; }
            public long AvgTicks { get; }
        }
    }
}