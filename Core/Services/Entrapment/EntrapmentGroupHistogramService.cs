using System.Collections;
using Core.Models;
using Core.Services.BioPolymer;
using Core.Util;
using Omics;
using Omics.Digestion;
using Proteomics.ProteolyticDigestion;
using Transcriptomics.Digestion;

namespace Core.Services.Entrapment;

public interface IEntrapmentGroupHistogramService
{
    void WriteModificationHistogram(IEnumerable<EntrapmentGroup> groups, string outputDirectory, string entrapmentDbName);
    public void WriteDigestionHistogram(IEnumerable<EntrapmentGroup> groups, string outputDirectory, IDigestionParams digestionParams, string entrapmentDbName);
}

/// <summary>
/// Determines fold by Shuffle_X in the name of the entrapment sequence. 
/// </summary>
/// <param name="modCalc"></param>
/// <param name="digCalc"></param>
public class EntrapmentGroupHistogramService(IModificationHistogramCalculator modCalc, IDigestionHistogramCalculator digCalc) : BaseService, IEntrapmentGroupHistogramService
{
    public void WriteModificationHistogram(IEnumerable<EntrapmentGroup> groups, string outputDirectory, string entrapmentDbName)
    {
        if (Verbose)
            Logger.WriteLine("Calculating Modification Histogram...");

        int count = 20000;
        if (groups is IList groupList)
            count = groupList.Count;

        var folds = new Dictionary<int, List<IBioPolymer>>();
        var targets = new List<IBioPolymer>(count);

        // Separate entrapments by fold and get targets
        foreach (var group in groups)
        {
            targets.Add(group.Target.BioPolymer);

            foreach (var entrap in group.Entrapments)
            {
                string[] nameParts = entrap.BioPolymer.Name.Split('_');
                int fold = 0;
                if (nameParts.Length > 0)
                    int.TryParse(nameParts[^1], out fold);

                if (!folds.TryGetValue(fold, out var list))
                {
                    list = new List<IBioPolymer>(count);
                    folds[fold] = list;
                }
                list.Add(entrap.BioPolymer);
            }
        }

        // Use _modCalc.GetModificationHistogram for targets and for each fold of entrapments
        var targetHist = modCalc.GetModificationHistogram(targets);
        var foldHists = new Dictionary<int, Dictionary<int, int>>();
        Parallel.ForEach(folds, kvp =>
        {
            var hist = modCalc.GetModificationHistogram(kvp.Value);
            lock (foldHists)
            {
                foldHists[kvp.Key] = hist;
            }
        });

        // Aggregate and write CSV
        var allKeys = new SortedSet<int>(targetHist.Keys);
        foreach (var hist in foldHists.Values)
            allKeys.UnionWith(hist.Keys);

        var outputPath = Path.Combine(outputDirectory, $"{entrapmentDbName}_ModificationHistogram.csv");
        using var writer = new StreamWriter(outputPath);
        writer.WriteLine("Modifications,Targets," + string.Join(",", foldHists.Keys.Select(f => $"Fold{f}")));
        foreach (var key in allKeys)
        {
            writer.Write(key);
            writer.Write(',');
            writer.Write(targetHist.TryGetValue(key, out var targetCount) ? targetCount : 0);
            foreach (var fold in foldHists.Keys)
            {
                writer.Write(',');
                var foldHist = foldHists[fold];
                writer.Write(foldHist.TryGetValue(key, out var foldCount) ? foldCount : 0);
            }
            writer.WriteLine();
        }

        // Add total row
        writer.Write("Total");
        writer.Write(',');
        var totalTarget = targetHist.Sum(kvp => kvp.Key * kvp.Value);
        writer.Write(totalTarget);
        foreach (var fold in foldHists.Keys)
        {
            writer.Write(',');
            var foldHist = foldHists[fold];
            var totalFold = foldHist.Sum(kvp => kvp.Key * kvp.Value);
            writer.Write(totalFold);
        }
        writer.WriteLine();

        if (Verbose)
            Logger.WriteLine($"Wrote modification histogram to {outputPath}");
    }

    public void WriteDigestionHistogram(IEnumerable<EntrapmentGroup> groups, string outputDirectory, IDigestionParams digestionParams, string entrapmentDbName)
    {
        if (Verbose)
            Logger.WriteLine("Calculating Digestion Product Histogram...");

        int count = 20000;
        if (groups is IList groupList)
            count = groupList.Count;

        var folds = new Dictionary<int, List<IBioPolymer>>();
        var targets = new List<IBioPolymer>(count);

        // Separate entrapments by fold and get targets
        foreach (var group in groups)
        {
            targets.Add(group.Target.BioPolymer);

            foreach (var entrap in group.Entrapments)
            {
                string[] nameParts = entrap.BioPolymer.Name.Split('_');
                int fold = 0;
                if (nameParts.Length > 0)
                    int.TryParse(nameParts[^1], out fold);

                if (!folds.TryGetValue(fold, out var list))
                {
                    list = new List<IBioPolymer>(count);
                    folds[fold] = list;
                }
                list.Add(entrap.BioPolymer);
            }
        }

        // Use _digCalc.GetDigestionHistogram for targets and for each fold of entrapments
        var targetHist = digCalc.GetDigestionHistogram(targets, digestionParams, out var targetMassHist);
        var foldHists = new Dictionary<int, Dictionary<int, int>>();
        var foldMassHists = new Dictionary<int, Dictionary<int, int>>();
        Parallel.ForEach(folds, kvp =>
        {
            var hist = digCalc.GetDigestionHistogram(kvp.Value, digestionParams, out var massHist);
            lock (foldHists)
            {
                foldHists[kvp.Key] = hist;
                foldMassHists[kvp.Key] = massHist;
            }
        });

        foldHists = foldHists.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        foldMassHists = foldMassHists.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Aggregate and write CSV
        var allKeys = new SortedSet<int>(targetHist.Keys);
        foreach (var hist in foldHists.Values)
            allKeys.UnionWith(hist.Keys);
        var outputPath = Path.Combine(outputDirectory, $"{entrapmentDbName}_DigestionHistogram.csv");
        using var writer = new StreamWriter(outputPath);
        writer.WriteLine($"{GetIdentifier(digestionParams)},Targets," + string.Join(",", foldHists.Keys.Select(f => $"Fold{f}")));
        foreach (var key in allKeys)
        {
            writer.Write(key);
            writer.Write(',');
            writer.Write(targetHist.TryGetValue(key, out var targetCount) ? targetCount : 0);
            foreach (var fold in foldHists.Keys)
            {
                writer.Write(',');
                var foldHist = foldHists[fold];
                writer.Write(foldHist.TryGetValue(key, out var foldCount) ? foldCount : 0);
            }
            writer.WriteLine();
        }

        // Add total row
        writer.Write("Total");
        writer.Write(',');
        var totalTarget = targetHist.Sum(kvp => kvp.Key * kvp.Value);
        writer.Write(totalTarget);
        foreach (var fold in foldHists.Keys)
        {
            writer.Write(',');
            var foldHist = foldHists[fold];
            var totalFold = foldHist.Sum(kvp => kvp.Key * kvp.Value);
            writer.Write(totalFold);
        }
        writer.WriteLine();

        if (Verbose)
            Logger.WriteLine($"Wrote digestion histogram to {outputPath}");

        // Aggregate and write CSV
        allKeys = new SortedSet<int>(targetMassHist.Keys);
        foreach (var hist in foldMassHists.Values)
            allKeys.UnionWith(hist.Keys);
        outputPath = Path.Combine(outputDirectory, $"{entrapmentDbName}_PrecursorMassHistogram.csv");
        using var writer2 = new StreamWriter(outputPath);
        writer2.WriteLine($"{GetIdentifier(digestionParams)},Targets," + string.Join(",", foldMassHists.Keys.Select(f => $"Fold{f}")));
        foreach (var key in allKeys)
        {
            writer2.Write(key);
            writer2.Write(',');
            writer2.Write(targetMassHist.TryGetValue(key, out var targetCount) ? targetCount : 0);
            foreach (var fold in foldMassHists.Keys)
            {
                writer2.Write(',');
                var foldHist = foldMassHists[fold];
                writer2.Write(foldHist.TryGetValue(key, out var foldCount) ? foldCount : 0);
            }
            writer2.WriteLine();
        }

        if (Verbose)
            Logger.WriteLine($"Wrote digestion histogram to {outputPath}");
    }

    private string GetIdentifier(IDigestionParams digestionParams)
    {
        var agentName = digestionParams.DigestionAgent.Name;
        return digestionParams switch
        {
            DigestionParams when agentName == "top-down" => "Proteoforms",
            DigestionParams => "Peptides",
            RnaDigestionParams when agentName == "top-down" => "Transcripts",
            RnaDigestionParams => "Oligos",
            _ => string.Empty
        };
    }
}
