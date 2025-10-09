using Core.Models;
using Core.Services.IO;
using Core.Util;
using Omics;
using UsefulProteomicsDatabases;

namespace Core.Services.Entrapment;

public interface IEntrapmentLoadingService : IBaseService
{
    DatabaseSet LoadAndParseProteins(IList<string> dbPaths);

}

public class EntrapmentLoadingService(IBioPolymerDbReader<IBioPolymer> dbReader) : BaseService, IEntrapmentLoadingService
{
    private readonly IBioPolymerDbReader<IBioPolymer> _dbReader = dbReader;
    public static BioPolymerDbReaderOptions DefaultDbReaderOptions
        => new()
        {
            DecoyType = DecoyType.None,
            DecoyIdentifier = "DECOY",
            AddTruncations = false,
            MaxHeterozygousVariants = 0,
            MinAlleleDepth = 0
        };

    public DatabaseSet LoadAndParseProteins(IList<string> dbPaths)
    {
        if (Verbose)
            Logger.WriteLine($"Loading {dbPaths.Count} databases...");

        // Load all databases 
        List<IBioPolymer> allBioPolymers = [];
        foreach (var dbPath in dbPaths)
        {
            var bioPolymers = _dbReader.Load(dbPath, DefaultDbReaderOptions);
            allBioPolymers.AddRange(bioPolymers);

            if (Verbose)
            {
                Logger.WriteLine($"\tLoaded {bioPolymers.Count} entries from {Path.GetFileNameWithoutExtension(dbPath)}");
                Logger.WriteLine($"\t\tTargets: {bioPolymers.Count(p => !p.IsEntrapment())}");
                Logger.WriteLine($"\t\tEntrapments: {bioPolymers.Count(p => p.IsEntrapment())}");
            }
        }

        // Parse into ProteinRecords
        List<BioPolymerRecord> allRecords = [];
        foreach (var bioPolymer in allBioPolymers)
        {
            bool target = !bioPolymer.IsDecoy();
            if (!target)
                continue;

            bool entrapment = bioPolymer.IsEntrapment();

            var record = new BioPolymerRecord()
            {
                BioPolymer = bioPolymer,
                IsTarget = target,
                IsEntrapment = entrapment
            };
            allRecords.Add(record);
        }

        // Group into ProteinGroups by target accession
        List<EntrapmentGroup> proteinGroups = [];
        var grouped = allRecords.GroupBy(r =>
            r.IsEntrapment ? r.BioPolymer.Accession.Split('_')[1] : r.BioPolymer.Accession);
        foreach (var group in grouped.OrderBy(p => p.Key))
        {
            var target = group.FirstOrDefault(r => r is { IsTarget: true, IsEntrapment: false });
            var entrapments = group.Where(r => r.IsEntrapment).ToList();
            if (target is null)
                throw new Exception($"No target found for group {group.Key}");

            if (entrapments.Count > 0)
            {
                proteinGroups.Add(new EntrapmentGroup(accession: group.Key, target: target, entrapments: entrapments));
            }
        }

        if (Verbose)
        {
            var targetCount = allBioPolymers.Count(p => !p.IsEntrapment());
            Logger.WriteLine($"Parsed {targetCount} target proteins into {proteinGroups.Count} groups with {proteinGroups.Average(p => p.Entrapments.Count):F2} entrapments per target.", 1);
            // Histogram of entrapments per target
            var hist = proteinGroups.GroupBy(g => g.Entrapments.Count)
                .OrderBy(g => g.Key)
                .Select(g => (Entrapments: g.Key, Targets: g.Count()));
            Logger.WriteLine("Entrapments per Target:", 2);
            foreach (var (entrapments, targets) in hist)
                Logger.WriteLine($"{entrapments}: {targets}", 3);
        }

        return new DatabaseSet(proteinGroups);
    }
}
