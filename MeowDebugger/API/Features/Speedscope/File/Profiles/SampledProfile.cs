using MeowDebugger.API.Features.Speedscope.File.Structs;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    /// <summary>
    /// Represents a profile that collects and organizes sampled data over a specified range, supporting weighted
    /// analysis of the samples.
    /// </summary>
    public class SampledProfile : BaseProfile
    {
        /// <inheritdoc/>
        [JsonConstructor]
        public SampledProfile(string name, string unit, long startValue, long endValue, List<List<long>> samples, List<double> weights) : base(name, unit, startValue, endValue)
        {
            this.Samples = samples;
            this.Weights = weights;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Sampled;

        /// <summary>
        /// Gets the list of samples, where each sample is represented as a list of frame indexes.
        /// </summary>
        [JsonPropertyName("samples")]
        public List<List<long>> Samples { get; } = [];

        /// <summary>
        /// Gets the list of weights corresponding to each sample.
        /// </summary>
        [JsonPropertyName("weights")]
        public List<double> Weights { get; } = [];
    }
}

