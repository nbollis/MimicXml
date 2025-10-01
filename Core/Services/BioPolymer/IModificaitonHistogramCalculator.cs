using Omics;

namespace Core.Services.BioPolymer;

public interface IModificationHistogramCalculator
{
    Dictionary<int, int> GetModificationHistogram(IEnumerable<IBioPolymer> polymers);
}

public class ModificationHistogramCalculator : BaseService, IModificationHistogramCalculator
{
    public Dictionary<int, int> GetModificationHistogram(IEnumerable<IBioPolymer> polymers)
    {
        var histogram = new Dictionary<int, int>();
        foreach (var polymer in polymers)
        {
            var modCount = polymer.OneBasedPossibleLocalizedModifications.Sum(kvp => kvp.Value.Count);
            if (!histogram.TryAdd(modCount, 1))
            {
                histogram[modCount]++;
            }
        }
        return histogram;
    }
}