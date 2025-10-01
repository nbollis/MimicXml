using Omics.Modifications;
using System.Reflection;

namespace Core.Util;

public static class Modifications
{
    public static List<Modification> AllProteinModsKnown;

    static Modifications()
    {
        // Use reflection to access the internal AllKnownMods field
        var field = typeof(Readers.ModificationConverter).GetField("AllKnownMods", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var value = field?.GetValue(null) as List<Modification>;
        AllProteinModsKnown = value != null
            ? new List<Modification>(value)
            : new List<Modification>();
    }
}
