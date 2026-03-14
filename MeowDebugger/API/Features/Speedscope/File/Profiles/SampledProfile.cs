using MeowDebugger.API.Features.Speedscope.File.Structs;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public class SampledProfile : BaseProfile
    {
        [JsonConstructor]
        public SampledProfile(string Name, string unit, long startValue, long endValue, List<List<long>> samples, List<double> weights) : base(Name, unit, startValue, endValue)
        {
            this.Samples = samples;
            this.Weights = weights;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Sampled;

        [JsonPropertyName("samples")]
        public List<List<long>> Samples { get; set; } = new();

        [JsonPropertyName("weights")]
        public List<double> Weights { get; set; } = new();
    }
}

