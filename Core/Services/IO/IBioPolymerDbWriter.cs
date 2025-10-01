using Omics;
using Omics.Modifications;

namespace Core.Services.IO;

public interface IBioPolymerDbWriter : IBaseService
{
    /// <summary>
    /// Write a database to disk.
    /// Uses %AGCTU content to detect protein or RNA,
    /// and file extension to choose XML or FASTA.
    /// </summary>
    void Write(
        List<IBioPolymer> bioPolymers,
        string outputFilePath,
        Dictionary<string, HashSet<Tuple<int, Modification>>>? additionalModsToAdd = null);
}