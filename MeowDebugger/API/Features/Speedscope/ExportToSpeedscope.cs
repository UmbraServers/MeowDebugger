using LabApi.Features.Console;
using LabApi.Loader;
using MeowDebugger.API.Features.Speedscope.File;
using MeowDebugger.API.Features.Speedscope.File.Profiles;
using MeowDebugger.API.Features.Speedscope.File.Structs;
using MeowDebugger.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static MeowDebugger.API.Features.MethodMetrics;

namespace MeowDebugger.API.Features.Speedscope;

/// <summary>
/// Provides functionality to export profiling data to a JSON file in a format compatible with Flamescope.
/// </summary>
public class ExportToSpeedscope
{
    /// <summary>
    /// Exports the json file.
    /// </summary>
    /// <returns>the json file</returns>
    public static bool ExportJsonFile(out string filePath)
    {
        filePath = string.Empty;

        if (Events == null || Events.Count == 0)
        {
            return false;
        }

        List<FrameEvent> events = [.. Events.OrderBy(e => e.At)];

        Dictionary<int, long> counts = [];
        List<List<long>> samples = [];
        List<long> weights = [];
        List<Frame> frames = Frames;

        foreach (FrameEvent frameEvent in events.Where(frameEvent => frameEvent.Type == EventType.CloseFrame))
        {
            if (!counts.ContainsKey(frameEvent.FrameIndex))
            {
                counts[frameEvent.FrameIndex] = 1;
                continue;
            }

            counts[frameEvent.FrameIndex] += 1;
        }

        // key is frame index and value is count
        foreach (var kvp in counts)
        {
            samples.Add([kvp.Key]);
            weights.Add(kvp.Value);
            frames[kvp.Key] = new Frame($"{Frames[kvp.Key].Name} (x{kvp.Value})", Frames[kvp.Key].File);
        }

        EventedProfile timeProfile = new("Time (ns)", ValueUnit.Nanoseconds, events.First().At, events.Last().At, events);
        SampledProfile countProfile = new("Call Count", ValueUnit.None, 0, samples.Count, samples, weights);

        SpeedscopeFile file = new([timeProfile, countProfile], new SharedFrames(frames), "MeowDebugger@1.0.0");

        Events.Clear();

        try
        {
            string jsonString = JsonConvert.SerializeObject(file, Formatting.Indented);
            string path = ConfigDebugger.Instance!.SpeedscopeOutputPath == string.Empty
#if EXILED_RELEASE
                ? Path.Combine(MeowDebuggerExiled.Instance!.ConfigPath, "Speedscope") : ConfigDebugger.Instance!.SpeedscopeOutputPath;
#else
                ? Path.Combine(MeowDebuggerLabAPI.Instance!.GetConfigDirectory().FullName, "Speedscope") : ConfigDebugger.Instance!.SpeedscopeOutputPath;
#endif

            Directory.CreateDirectory(path);


            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string namespaces = string.Join("-", ConfigDebugger.Instance!.WhitelistNamespaces);


            string filename = $"{namespaces}_{timestamp}.json";
            filePath = Path.Combine(path, filename);
            System.IO.File.WriteAllText(filePath, jsonString);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
    }
}