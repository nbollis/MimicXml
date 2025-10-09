using Omics;

namespace Core.Models;

/// <summary>
/// A single protein record, either target or entrapment.
/// </summary>
public class BioPolymerRecord()
{
    public string Accession => BioPolymer.Accession;
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

    public void AddShuffleNumberToAccession()
    {
        for (int i = 0; i < Entrapments.Count; i++)
        {
            var entrapment = Entrapments[i];
            var shuffleNum = entrapment.BioPolymer.Name.Split("shuffle_", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();

            if (shuffleNum is null)
                throw new Exception($"Could not determine shuffle number for entrapment {entrapment.Accession} with name {entrapment.BioPolymer.Name}");

            string newAccession = entrapment.Accession + $"_{shuffleNum}";

            // Use reflection to set the read-only Accession property
            var accessionProp = entrapment.BioPolymer.GetType().GetProperty("Accession");
            if (accessionProp is not null && accessionProp.CanWrite)
            {
                accessionProp.SetValue(entrapment, new string(newAccession));
            }
            else 
            {
                // Set the Accession property via a backing field if possible
                var bioPolymerType = entrapment.BioPolymer.GetType();
                var accessionField = bioPolymerType.GetField("<Accession>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (accessionField is not null)
                {
                    accessionField.SetValue(entrapment.BioPolymer, newAccession);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot set Accession backing field on BioPolymer of type {bioPolymerType.FullName}");
                }
            }
        }
    }
}