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
        var aggregated = new Dictionary<string, (long ticks, int count, MethodBase[] stack)>();

        foreach (var stackEntry in MethodMetrics.StackValue)
        {
            var key = string.Join(";", FullName(stackEntry.Method));

            if (aggregated.TryGetValue(key, out var existing))
                aggregated[key] = (existing.ticks + stackEntry.ChildTicks, existing.count + 1, existing.stack);
            else
                aggregated[key] = (stackEntry.ChildTicks, 1, MethodMetrics.StackValue.Select(s => s.Method).ToArray());
        }

        var methodCounts = new Dictionary<string, int>();
        foreach (var entry in aggregated.Values)
        {
            // Only count the leaf (last frame) to avoid inflating parent counts
            var leafName = FullName(entry.stack[entry.stack.Length - 1]);
            if (methodCounts.TryGetValue(leafName, out var c))
                methodCounts[leafName] = c + entry.count;
            else
                methodCounts[leafName] = entry.count;
        }


        List<Frame> frames = new List<Frame>();
        var frameNameIndex = new Dictionary<string, int>();

        int GetFrame(MethodBase m)
        {
            var name = FullName(m);
            if (!frameNameIndex.TryGetValue(name, out var idx))
            {
                idx = frames.Count;
                frameNameIndex[name] = idx;

                var count = methodCounts.TryGetValue(name, out var cnt) ? cnt : 0;
                frames.Add(new Frame
                {
                    name = count > 0 ? $"{name} (x{count})" : name
                });
            }
            return idx;
        }

        foreach (var entry in aggregated.Values)
            foreach (var method in entry.stack)
                GetFrame(method);

        var nameAggregated = new Dictionary<string, (long ticks, int count)>();
        foreach (var kv in aggregated)
            nameAggregated[kv.Key] = (kv.Value.ticks, kv.Value.count);

        var selfTimes = ComputeSelfTimes(nameAggregated);

        var sampleStacks = new List<List<long>>();
        var timeWeights = new List<long>();
        var countWeights = new List<long>();

        foreach (var kv in aggregated)
        {
            if (!selfTimes.TryGetValue(kv.Key, out var self)) continue;
            if (self.selfUs < 2) continue;

            var frameStack = new List<long>(kv.Value.stack.Length);
            foreach (var m in kv.Value.stack)
                frameStack.Add(GetFrame(m));

            sampleStacks.Add(frameStack);
            timeWeights.Add(self.selfUs);
            countWeights.Add(kv.Value.count);
        }

        if (sampleStacks.Count == 0)
        {
            return false;
        }

        List<SampledProfile> profiles = new List<SampledProfile>();
        Shared shared = new Shared(frames);

        SampledProfile timeProfile = new SampledProfile("Time", ValueUnit.Microseconds, 0, timeWeights.Sum(), sampleStacks, timeWeights);

        try
        {
            var file = new SpeedScopeFile(shared, profiles, "something2");

            string jsonString = JsonConvert.SerializeObject(file, Formatting.Indented);

            string path = Path.Combine(MeowDebuggerLabAPI.Instance.GetConfigDirectory().FullName, "Speescope");

            Directory.CreateDirectory(path);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"profile_{timestamp}.json";
            string filePath = Path.Combine(path, filename);
            System.IO.File.WriteAllText(path, jsonString);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static Dictionary<string, (long selfUs, int count)> ComputeSelfTimes(
    Dictionary<string, (long ticks, int count)> aggregated)
    {
        var childrenOf = new Dictionary<string, long>();
        foreach (var kv in aggregated)
        {
            var lastSep = kv.Key.LastIndexOf(';');
            if (lastSep < 0) continue;

            var parentKey = kv.Key.Substring(0, lastSep);
            long childUs = (long)TicksToMicroseconds(kv.Value.ticks);

            if (childrenOf.TryGetValue(parentKey, out var existing))
                childrenOf[parentKey] = existing + childUs;
            else
                childrenOf[parentKey] = childUs;
        }

        var result = new Dictionary<string, (long selfUs, int count)>();
        foreach (var kv in aggregated)
        {
            long totalUs = (long)TicksToMicroseconds(kv.Value.ticks);
            long childUs = childrenOf.TryGetValue(kv.Key, out var c) ? c : 0;
            long selfUs = Math.Max(0, totalUs - childUs);

            result[kv.Key] = (selfUs, kv.Value.count);
        }

        return result;
    }

    private static readonly double TickToMicro =
    1_000_000.0 / Stopwatch.Frequency;

    private static double TicksToMicroseconds(long ticks)
        => ticks * TickToMicro;

    private static string FullName(MethodBase method) => method.DeclaringType != null ? $"{method.DeclaringType.FullName}.{method.Name}" : method.Name;

}