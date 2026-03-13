using MeowDebugger.API.Features.Speedscope.File.Structs;

namespace MeowDebugger.API.Features.Speedscope.File.Profiles
{
    public class SampledProfile : BaseProfile
    {
        // Too lazy to implement it, I was looking at the speedscope code but who cares I'm not going to use this shit

        public SampledProfile(string Name, string unit, long startValue, long endValue) : base(Name, unit, startValue, endValue)
        {
        }

        public override string Type => ProfileType.Sampled;
    }
}
