using Microsoft.ML.Data;
using Omics;
using Omics.Digestion;
using Proteomics.ProteolyticDigestion;

namespace Core.Services.BioPolymer;

public interface IDigestionHistogramCalculator
{
    Dictionary<int, int> GetDigestionHistogram(IEnumerable<IBioPolymer> polymers, IDigestionParams digestionParams, out Dictionary<int, int> massHistogram);
}

public class DigestionHistogramCalculator : BaseService, IDigestionHistogramCalculator
{
    public Dictionary<int, int> GetDigestionHistogram(IEnumerable<IBioPolymer> polymers, IDigestionParams digestionParams, out Dictionary<int, int> massHistogram)
    {
        var histogram = new Dictionary<int, int>();
        massHistogram = new Dictionary<int, int>();
        foreach (var polymer in polymers)
        {
            var digested = polymer.Digest(digestionParams, [], []);
            int pepCount = 0;
            foreach (var pep in digested)
            {
                pepCount++;
                if (!massHistogram.TryAdd((int)pep.MonoisotopicMass, 1))
                {
                    massHistogram[(int)pep.MonoisotopicMass]++;
                }
            }
            if (!histogram.TryAdd(pepCount, 1))
            {
                histogram[pepCount]++;
            }
        }
        return histogram;
    }
}