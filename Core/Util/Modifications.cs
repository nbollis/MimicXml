using Omics.Modifications;
using System.Reflection;

namespace Core.Util;

public static class Modifications
{
    public static readonly List<Modification> AllProteinModsKnown;

    static Modifications()
    {
        // Use reflection to access the internal AllKnownMods field
        var field = typeof(Readers.ModificationConverter).GetField("AllKnownMods", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        AllProteinModsKnown = field?.GetValue(null) is List<Modification> value
            ? [..value]
            : [];
    }
}
