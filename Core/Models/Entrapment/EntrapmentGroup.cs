using MzLibUtil;
using Omics;

namespace Core.Models.Entrapment;

/// <summary>
/// A single protein record, either target or entrapment.
/// </summary>
public class BioPolymerRecord(string? customAccession = null)
{
    public string Accession => customAccession ?? BioPolymer.Accession;
    public bool IsTarget { get; init; }
    public bool IsEntrapment { get; init; }
    public IBioPolymer BioPolymer { get; init; }
    public double BestScore { get; set; } = double.PositiveInfinity;
}

/// <summary>
/// A target protein and its associated entrapment proteins.
/// </summary>
public class EntrapmentGroup(string accession, BioPolymerRecord target, List<BioPolymerRecord> entrapments)
{
    private static readonly ListPool<BioPolymerRecord> _listPool = new();
    public string Accession { get; set; } = accession;
    public BioPolymerRecord Target { get; init; } = target;
    public List<BioPolymerRecord> Entrapments { get; init; } = entrapments;
    public int EntrapmentsFound => Entrapments.Count(p => !double.IsPositiveInfinity(p.BestScore) && !double.IsNegativeInfinity(p.BestScore));

    public bool TryGetRecord(string accession, out BioPolymerRecord? record)
    {
        if (Target.Accession.Equals(accession, StringComparison.OrdinalIgnoreCase))
        {
            record = Target;
            return true;
        }
        record = Entrapments.FirstOrDefault(e => e.Accession.Equals(accession, StringComparison.OrdinalIgnoreCase));

        var splits = accession.Split('_');
        if (splits.Length == 4) // Mimic accession plus an extra section from MM 
            record = Entrapments.FirstOrDefault(e => e.Accession.Equals($"{splits[0]}_{splits[1]}_{splits[2]}", StringComparison.OrdinalIgnoreCase));

        return record != null;
    }


    /// <summary>
    /// Get all records (target + entrapments) sorted by BestScore, breaking ties randomly.
    /// Only records with BestScore <= alpha are sorted by score; the rest are ordered randomly.
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="alpha">Cutoff above which results are ordered randomly</param>
    /// <returns></returns>
    public IEnumerable<BioPolymerRecord> GetSortedPiDelta(Random rand, double alpha)
    {
        // Avoid repeated Concat and Where by using a single loop
        var belowAlpha = _listPool.Get();
        var aboveAlpha = _listPool.Get();

        foreach (var record in Entrapments)
        {
            if (record.BestScore <= alpha)
                belowAlpha.Add(record);
            else
                aboveAlpha.Add(record);
        }
        // Include Target
        if (Target.BestScore <= alpha)
            belowAlpha.Add(Target);
        else
            aboveAlpha.Add(Target);

        // Sort belowAlpha by BestScore, then random
        belowAlpha.Sort((a, b) =>
        {
            int cmp = a.BestScore.CompareTo(b.BestScore);
            if (cmp != 0) return cmp;
            return rand.Next(-1, 2); // Random tie-breaker (-1, 0, 1)
        });

        // Shuffle aboveAlpha randomly
        for (int i = aboveAlpha.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (aboveAlpha[i], aboveAlpha[j]) = (aboveAlpha[j], aboveAlpha[i]);
        }

        foreach (var r in belowAlpha)
            yield return r;
        foreach (var r in aboveAlpha)
            yield return r;

        _listPool.Return(belowAlpha);
        _listPool.Return(aboveAlpha);
    }
}