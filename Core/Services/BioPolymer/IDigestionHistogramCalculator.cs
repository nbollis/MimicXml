using Omics;
using Omics.Digestion;
using Proteomics.ProteolyticDigestion;

namespace Core.Services.BioPolymer;

public interface IDigestionHistogramCalculator
{
    Dictionary<int, int> GetDigestionHistogram(IEnumerable<IBioPolymer> polymers, IDigestionParams digestionParams);
}

public class DigestionHistogramCalculator : BaseService, IDigestionHistogramCalculator
{
    public Dictionary<int, int> GetDigestionHistogram(IEnumerable<IBioPolymer> polymers, IDigestionParams digestionParams)
    {
        var histogram = new Dictionary<int, int>();
        foreach (var polymer in polymers)
        {
            var digested = polymer.Digest(digestionParams, [], []);
            var pepCount = digested.Count();
            if (!histogram.TryAdd(pepCount, 1))
            {
                histogram[pepCount]++;
            }
        }
        return histogram;
    }
}