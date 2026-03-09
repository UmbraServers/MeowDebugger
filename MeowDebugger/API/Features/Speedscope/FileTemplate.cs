using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MeowDebugger.API.Features.Speedscope;

/// <summary>
/// Represents the class for the Speedscope's file format.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class FileTemplate
{
    /// <summary>
    /// Represents the struct for the <see cref="File"/>.
    /// </summary>
    [Serializable]
    public struct File
    {
        /// <summary>
        /// The file's schema.
        /// </summary>
        [JsonProperty("$schema")]
        public string schema = "https://www.speedscope.app/file-format-schema.json";

        /// <summary>
        /// List of profile definitions
        /// </summary>
        public List<SampledProfile> profiles;

        /// <inheritdoc/>
        public Shared shared;
       
        /// <summary>
        /// The index into the `profiles` array that should be displayed upon file load. If omitted, will default to displaying the first profile in the file.
        /// </summary>
        public int? activeProfileIndex;
        
        /// <summary>
        /// The name of the program which exported this profile. This isn't
        /// consumed but can be helpful for debugging generated data by seeing what
        /// was generating it! Recommended format is "name@version". e.g. when the
        /// file was exported by speedscope v0.6.0 itself, it will be
        /// </summary>
        public string? exporter = "Meow Debugger.";

        /// <summary>
        /// The name of the contained profile group. If omitted, will use the name of the file itself.
        /// </summary>
        public string? name;

        /// <summary>
        /// Creates an instance of <see cref="File"/>
        /// </summary>
        /// <param name="shared"><see cref="shared"/></param>
        /// <param name="profiles"><see cref="profiles"/></param>
        /// <param name="name"><see cref="name"/></param>
        public File(Shared shared, List<SampledProfile> profiles, string? name)
        {
            this.shared = shared;
            this.profiles = profiles;
            this.name = name;
        }
    }

    /// <summary>
    /// Represents the struct for the shared data profiles frames.
    /// </summary>
    [Serializable]
    public struct Shared
    {
        /// <summary>
        /// The list of <see cref="Frame"/>.
        /// </summary>
        public List<Frame> frames;

        /// <summary>
        /// Creates an instance of <see cref="Shared"/>
        /// </summary>
        /// <param name="frames"><see cref="frames"/></param>
        public Shared(List<Frame> frames)
        {
            this.frames = frames;
        }
    }

    /// <summary>
    /// Represents the struct for the <see cref="SampledProfile"/>.
    /// </summary>
    [Serializable]
    public struct SampledProfile
    {
        /// <summary>
        /// Type of profile. This will future-proof the file format to allow many
        /// different kinds of profiles to be contained and each type to be part of
        /// a discriminated union.
        /// </summary>
        public string type = "sampled";
        
        /// <summary>
        /// Name of the profile. Typically, a filename for the source of the profile.
        /// </summary>
        public string name;
        
        /// <summary>
        /// Unit which all value are specified using in the profile.
        /// </summary>
        public string unit;
        
        /// <summary>
        /// The starting value of the profile. This will typically be a timestamp.
        /// All event values will be displayed relative to this startValue.
        /// </summary>
        public long startValue;
        
        /// <summary>
        /// The final value of the profile. This will typically be a timestamp. This
        /// must be greater than or equal to the startValue. This is useful in
        /// situations where the recorded profile extends past the end of the recorded
        /// events, which may happen if nothing was happening at the end of the
        /// profile.
        /// </summary>
        public long endValue;
        
        /// <summary>
        /// List of the sampled stacks.
        /// </summary>
        public List<List<long>> samples; 
        
        /// <summary>
        /// The weight of the sample at the given index. Should have
        /// the same length as the samples array.
        /// </summary>
        public List<long> weights;
        
        /// <summary>
        /// Creates an instance of <see cref="SampledProfile"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="unit"></param>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <param name="samples"></param>
        /// <param name="weights"></param>
        public SampledProfile(string name, string unit, long startValue, long endValue, List<List<long>> samples, List<long> weights)
        {
            this.name = name;
            this.unit = unit;
            this.startValue = startValue;
            this.endValue = endValue;
            this.samples = samples;
            this.weights = weights;
        }
    }

    /// <summary>
    /// Represents the struct for the <see cref="Frame"/>.
    /// </summary>
    [Serializable]
    public struct Frame
    {
        /// <summary>
        /// The frame's name.
        /// </summary>
        public string name;

#pragma warning disable CS1591
        public string? file;

        public long? line;

        public long? col;
#pragma warning restore CS1591

        /// <summary>
        /// Creates an instance of <see cref="Frame"/>
        /// </summary>
        /// <param name="name"></param>
        public Frame(string name)
        {
            this.name = name;
        }
    }
    /// <summary>
    /// Represents the enum for the value unit of the <see cref="SampledProfile"/>.
    /// </summary>
    [Serializable]
    public enum ValueUnit
    {
#pragma warning disable CS1591
        bytes,
        microseconds,
        milliseconds,
        nanoseconds,
        none,
        seconds,
#pragma warning restore CS1591
    }
}