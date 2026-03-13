using MeowDebugger.API.Features.Speedscope.File.Structs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public class SampledProfile : BaseProfile
    {
        public SampledProfile(string Name, string unit, long startValue, long endValue, List<List<long>> samples, List<long> weights) : base(Name, unit, startValue, endValue)
        {
            this.Samples = samples;
            this.Weights = weights;
        }

        /// <inheritdoc/>
        public override string Type => ProfileType.Sampled;

        [JsonProperty("samples")]
        public List<List<long>> Samples { get; set; } = new();

        [JsonProperty("weights")]
        public List<long> Weights { get; set; } = new();
    }
}

