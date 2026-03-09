using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using UnityEngine;
using static MeowDebugger.API.Features.Speedscope.FileTemplate;
using File = MeowDebugger.API.Features.Speedscope.FileTemplate.File;
using Logger = LabApi.Features.Console.Logger;

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
    public static string ExportJsonFile()
    {
        List<Frame> frames =
        [
            new Frame("f0"),
        new Frame("f1"),
        new Frame("f2"),
        new Frame("f3"),
        new Frame("f4")
        ];

        Shared shared = new Shared(frames);

        List<SampledProfile> profiles =
        [
            new SampledProfile(
            "something1",
            ValueUnit.milliseconds.ToString(),
            0,
            2,
            [
                [2,3],
                [3,4]
            ],
            [1,2]
        )
        ];

        var file = new File(shared, profiles, "something2");

        string jsonString = JsonConvert.SerializeObject(file, Formatting.Indented);

        Logger.Info(jsonString);
        return jsonString;
    }
}