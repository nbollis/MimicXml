using System.Collections;

namespace Core.Models.Entrapment;

/// <summary>
/// Represents all proteins in a database, grouped by target and associated entrapments.
/// </summary>
public class DatabaseSet : IEnumerable<EntrapmentGroup>
{
    public int K { get; }
    public int TargetCount { get; private set; }
    public int EntrapmentCount { get; private set; }
    public List<EntrapmentGroup> Proteins { get; private set; }

    // Fast lookups
    private readonly Dictionary<string, EntrapmentGroup> _lookup;

    public DatabaseSet(List<EntrapmentGroup> groups)
    {
        Proteins = groups;
        TargetCount = groups.Count;
        EntrapmentCount = groups.Sum(g => g.Entrapments.Count);
        K = (int)Math.Round(EntrapmentCount / (double)TargetCount, MidpointRounding.AwayFromZero);

        _lookup = new Dictionary<string, EntrapmentGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            // Target accession maps directly
            _lookup[group.Accession] = group;
        }
    }

    public IEnumerable<EntrapmentGroup> GetThoseWithResultBelowCutoff(double cutoff)
    {
        return Proteins.Where(g => g.Target.BestScore <= cutoff || g.Entrapments.Any(e => e.BestScore <= cutoff));
    }

    public EntrapmentGroup? GetGroupForAccession(string accession)
    {
        // Try exact, or normalized entrapment
        if (_lookup.TryGetValue(accession, out var group))
            return group;

        var norm = NormalizeEntrapmentAccession(accession);
        return _lookup.GetValueOrDefault(norm);
    }

    private static string NormalizeEntrapmentAccession(string acc)
    {
        // Handles Random_P84243_3 → P84243
        var parts = acc.Split('_');
        return parts.Length >= 2 ? parts[1] : acc;
    }

    public void Reset()
    {
        foreach (var group in Proteins)
        {
            group.Target.BestScore = double.PositiveInfinity;
            foreach (var e in group.Entrapments)
                e.BestScore = double.PositiveInfinity;
        }
    }

    public IEnumerator<EntrapmentGroup> GetEnumerator() => Proteins.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}