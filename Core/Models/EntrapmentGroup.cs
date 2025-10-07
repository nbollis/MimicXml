using Omics;

namespace Core.Models;

/// <summary>
/// A single protein record, either target or entrapment.
/// </summary>
public class BioPolymerRecord(string? customAccession = null)
{
    public string Accession => customAccession ?? BioPolymer.Accession;
    public bool IsTarget { get; init; }
    public bool IsEntrapment { get; init; }
    public required IBioPolymer BioPolymer { get; init; }
}

/// <summary>
/// A target protein and its associated entrapment proteins.
/// </summary>
public class EntrapmentGroup(string accession, BioPolymerRecord target, List<BioPolymerRecord> entrapments)
{
    public string Accession { get; } = accession;
    public BioPolymerRecord Target { get; } = target;
    public List<BioPolymerRecord> Entrapments { get; } = entrapments;
}