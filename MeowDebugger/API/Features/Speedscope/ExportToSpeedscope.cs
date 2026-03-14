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

        if (FrameEvents == null || FrameEvents.Count == 0)
        {
            return false;
        }

        List<FrameEvent> frameEvents = [.. FrameEvents.OrderBy(e => e.At)];
        List<Frame> frames = [.. Frames];

        EventedProfile timeProfile = new("Time (ns)", ValueUnit.Nanoseconds, frameEvents.First().At, frameEvents.Last().At, frameEvents);
        SampledProfile countProfile = CreateCountProfile(frameEvents, frames, out Dictionary<int, long> counts);
        SampledProfile timedProfile = CreateAverageMethodTimeProfile(FrameEvents, counts);

        SpeedscopeFile file = new([timeProfile, countProfile, timedProfile], new SharedFrames(frames), "MeowDebugger@1.0.0");

        FrameEvents.Clear();
        MethodIndexes.Clear();
        Frames.Clear();

        try
        {
            // TODO: Check the generated json and if something is wrong, show actually wtf is going on with
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

    private static SampledProfile CreateCountProfile(List<FrameEvent> frameEvents, List<Frame> frames, out Dictionary<int, long> counts)
    {
        List<List<long>> samples = [];
        List<double> weights = [];
        counts = [];

        foreach (FrameEvent frameEvent in frameEvents.Where(frameEvent => frameEvent.Type == FrameEventType.CloseFrame))
        {
            if (!counts.ContainsKey(frameEvent.FrameIndex))
            {
                counts[frameEvent.FrameIndex] = 1;
                continue;
            }

            counts[frameEvent.FrameIndex] += 1;
        }

        // TODO: Calculate self count and total count 
        // key is frame index and value is count
        foreach (var kvp in counts)
        {
            samples.Add([kvp.Key]);
            weights.Add(kvp.Value);
            frames[kvp.Key] = new Frame($"{Frames[kvp.Key].Name} (x{kvp.Value})", Frames[kvp.Key].File);
        }

        return new("Call Count", ValueUnit.None, 0, samples.Count, samples, weights);
    }

    private static SampledProfile CreateAverageMethodTimeProfile(List<FrameEvent> frameEvents, Dictionary<int, long> counts)
    {
        // key is frame index and value is average time (event final - event start)
        Dictionary<int, double> averageFrameTime = [];

        for (int i = 0; i < frameEvents.Count; i++)
        {
            if (i % 2 != 0 || i + 1 > frameEvents.Count)
            {
                continue;
            }

            FrameEvent openFrame = frameEvents[i];
            FrameEvent closedFrame = frameEvents[i + 1];

            if (openFrame.FrameIndex != closedFrame.FrameIndex)
            {
                Logger.Info("please check this shit inside the file: " + frameEvents[openFrame.FrameIndex] + " and " + frameEvents[closedFrame.FrameIndex]);
                continue;
            }

            if (!averageFrameTime.ContainsKey(openFrame.FrameIndex))
            {
                averageFrameTime[openFrame.FrameIndex] = 0;
            }

            averageFrameTime[openFrame.FrameIndex] += closedFrame.At - openFrame.At;
        }

        List<List<long>> samples = [];
        List<double> weights = [];

        foreach (var kvp in averageFrameTime)
        {
            samples.Add([kvp.Key]);
            weights.Add(kvp.Value / counts[kvp.Key]);
        }

        return new("Average Method Time (ns)", ValueUnit.Nanoseconds, 0, samples.Count, samples, weights);
    }
}