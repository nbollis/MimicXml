using System.Collections;

namespace Core.Models;

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

    public IEnumerator<EntrapmentGroup> GetEnumerator() => Proteins.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}