using Omics.Modifications;

namespace Core.Models;

public class ModInfo
{
    public char Residue { get; init; }

    /// <summary>
    /// Index of the Mod in the BioPolymer Dictionary. Not the position in the sequence.
    /// </summary>
    public int Position { get; init; }
    public Modification Mod { get; init; }
}