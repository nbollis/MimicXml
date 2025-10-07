using Omics.Modifications;

namespace Core.Models;

public class ModInfo
{
    public char Residue { get; init; }
    public int Position { get; init; }
    public Modification Mod { get; init; }
}