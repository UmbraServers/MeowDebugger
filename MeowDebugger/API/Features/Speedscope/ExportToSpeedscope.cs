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
using System.Reflection;
using static MeowDebugger.API.Features.MethodMetrics;

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
    public static bool ExportJsonFile(out string filePath)
    {
        (MethodBase method, Stats.Snapshot snapshot)[] snapshots = SnapshotAllAndReset();

        filePath = string.Empty;

        if (Events == null || Events.Count == 0)
        {
            return false;
        }

        List<FrameEvent> events = Events.OrderBy(e => e.At).ToList();

        var samples = new List<List<long>>();
        var weights = new List<long>();

        foreach ((MethodBase method, Stats.Snapshot snapshot) in snapshots)
        {
            if (snapshot.Count == 0)
                continue;

            // I'm taking this data from Zero's implementation, idk but I'm pretty sure something is borked there and I'm too lazy to check it :trollface:
            int frameIndex = Patcher.GetMethodIndex(method);

            samples.Add(new List<long> { frameIndex });
            weights.Add(snapshot.Count);
        }

        EventedProfile timeProfile = new("Time (ns)", ValueUnit.Nanoseconds, events.First().At, events.Last().At, events);
        SampledProfile countProfile = new("Call Count (MIGHT BE INNACURATE!!!)", ValueUnit.None, 0, samples.Count, samples, weights);

        //List<Frame> frames = new();

        //foreach (Frame frame in Patcher.Frames)
        //{
        //    if (Events.Select(frameEvent => frameEvent.FrameIndex).Contains(frame.Index))
        //    {
        //        frames.Add(frame);
        //    }
        //}

        //if (frames.Count == 0)
        //{
        //    Logger.Warn("No frames to export. PLEASE CONTACT UNBISTRACKTED!!!!!!! or make sure you are patching the right namespaces");
        //    return false;
        //}


        // TODO: Reduce file size, I need to update all the indexes for each event and that's annoying, I might do a pr in the original speedscope repo so I can get it from an key called "index", but idk
        SpeedscopeFile file = new SpeedscopeFile([timeProfile, countProfile], new SharedFrames(Patcher.Frames), "MeowDebugger@1.0.0");

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