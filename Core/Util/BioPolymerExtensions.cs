using Omics;

namespace Core.Util;

public static class BioPolymerExtensions
{
    public static bool IsDecoy(this string accession, string decoyIdentifier = "decoy_")
    {
        return accession.ToLower().Contains(decoyIdentifier);
    }

    public static bool IsDecoy(this IBioPolymer bioPolymer, string decoyIdentifier = "decoy_")
    {
        return bioPolymer.Accession.IsDecoy(decoyIdentifier);
    }

    private static readonly string[] _entrapmentIdentifiers = ["mimic", "random", "shuffle", "ntrap"];
    public static bool IsEntrapment(this string accession)
    {
        var lowerAccession = accession.ToLower();
        return _entrapmentIdentifiers.Any(id => lowerAccession.Contains('_') && lowerAccession.Contains(id));
    }

    public static bool IsEntrapment(this IBioPolymer bioPolymer)
    {
        return bioPolymer.Accession.IsEntrapment();
    }
}