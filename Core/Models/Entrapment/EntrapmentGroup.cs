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
    public string Accession { get; set; } = accession;
    public BioPolymerRecord Target { get; init; } = target;
    public List<BioPolymerRecord> Entrapments { get; init; } = entrapments;
}