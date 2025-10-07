using Core.Services;
using Core.Services.Entrapment;
using Core.Services.IO;
using Easy.Common.Extensions;
using Omics;
using Omics.Digestion;
using Omics.Modifications;
using System.Diagnostics;
using Core.Models;

namespace MimicXml;

public class EntrapmentXmlGenerator(IEntrapmentLoadingService loadingService, IBioPolymerDbWriter writingService, IEntrapmentGroupHistogramService histogramService) : BaseService
{
    public static string GetOutputPath(string entrapmentFastaPath) 
    {
        var dir = Path.GetDirectoryName(entrapmentFastaPath);
        var filename = Path.GetFileNameWithoutExtension(entrapmentFastaPath);
        return Path.Combine(dir ?? "", filename + "_Entrapment.xml");
    }

    /// <summary>
    /// Takes an xml and an entrapment database and produces an entrapment xml with all mods. 
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public void GenerateXml(string startingXmlPath, string entrapmentFastaPath, bool writeModHist, bool writeDigHist, IDigestionParams? digParams = null, string? outPath = null)
    {
        List<string> errors = new();
        loadingService.Verbose = Verbose;
        writingService.Verbose = Verbose;

        ValidateInputPaths(startingXmlPath, entrapmentFastaPath);

        var groups = loadingService.LoadAndParseProteins([startingXmlPath, entrapmentFastaPath]);
        foreach (var cluster in groups)
        {
            // Transfer all modifications. 
            var mods = ExtractTargetModifications(cluster.Target.BioPolymer)
                .OrderBy(p => p.Position)
                .ToList();

            foreach (var entrapment in cluster.Entrapments.Select(e => e.BioPolymer))
            {
                AssignModificationsByResidue(entrapment, cluster.Target.BioPolymer, mods, ref errors);
            }

            // Transfer truncations. 
            if (cluster.Target.BioPolymer.TruncationProducts.IsNotNullOrEmpty())
                foreach (var entrapment in cluster.Entrapments.Select(e => e.BioPolymer))
                    entrapment.TruncationProducts.AddRange(cluster.Target.BioPolymer.TruncationProducts);
        }

        var toWrite = groups.SelectMany(g => g.Entrapments.Select(e => e.BioPolymer))
            .ToList();

        outPath ??= GetOutputPath(startingXmlPath); 
        writingService.Write(toWrite, outPath);        

        if (writeDigHist || writeModHist)
        {
            groups = loadingService.LoadAndParseProteins([startingXmlPath, outPath]);

            // Print entrapment modification distribution for each entrapment fold
            if (writeModHist)
                histogramService.WriteModificationHistogram(groups, Path.GetDirectoryName(outPath) ?? "",
                    Path.GetFileNameWithoutExtension(outPath));

            // Print Digestion Counts
            if (writeDigHist && digParams != null)
                histogramService.WriteDigestionHistogram(groups, Path.GetDirectoryName(outPath) ?? "", digParams,
                    Path.GetFileNameWithoutExtension(outPath));
        }
    }

    public static void ValidateInputPaths(string startingXmlPath, string entrapmentFastaPath)
    {
        if (!startingXmlPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Starting database must be .xml ");
        if (!entrapmentFastaPath.EndsWith(".fasta", StringComparison.OrdinalIgnoreCase) &&
            !entrapmentFastaPath.StartsWith(".fa", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Entrapment database must be .fasta or .fa");
    }

    internal IEnumerable<ModInfo> ExtractTargetModifications(IBioPolymer target)
    {
        foreach (var (position, modList) in target.OneBasedPossibleLocalizedModifications)
        {
            foreach (var mod in modList.DistinctBy(p => p.IdWithMotif))
            {
                char residueAtSite = target.BaseSequence[position - 1];
                char motiff = mod.Target.ToString()[0];

                // Error Checks
                if (residueAtSite != motiff && motiff != 'X')
                    Debugger.Break(); // Should not happen

                if (position > target.BaseSequence.Length)
                    Debugger.Break(); // Should not happen

                // Mod can be on any residue, so use the actual residue at the site. 
                var res = motiff == 'X' ? residueAtSite : motiff;

                yield return new ModInfo
                {
                    Residue = res,
                    Position = position,
                    Mod = mod
                };
            }
        }
    }

    private void AssignModificationsByResidue(
        IBioPolymer entrapment,
        IBioPolymer target,
        List<ModInfo> mods,
        ref List<string> errors)
    {
        // Separate terminal and non-terminal mods
        var nTermMods = mods.Where(m =>
            m.Mod.LocationRestriction.Contains("N-term", StringComparison.InvariantCultureIgnoreCase) ||
            m.Mod.LocationRestriction.Contains("5'-", StringComparison.InvariantCultureIgnoreCase)).ToList();

        var cTermMods = mods.Where(m =>
            m.Mod.LocationRestriction.Contains("C-term", StringComparison.InvariantCultureIgnoreCase) ||
            m.Mod.LocationRestriction.Contains("3'-", StringComparison.InvariantCultureIgnoreCase)).ToList();

        var nonTermMods = mods.Except(nTermMods).Except(cTermMods).ToList();

        // Handle N-term mods
        foreach (var mod in nTermMods)
        {
            int pos = FindBestModPosition(entrapment, target, mod, ref errors);
            if (pos > 0)
                AddModToEntrapment(entrapment, pos, mod.Mod);
            else
                Debugger.Break();
        }

        // Handle C-term mods
        foreach (var mod in cTermMods)
        {
            int pos = FindBestModPosition(entrapment, target, mod, ref errors);
            if (pos > 0)
                AddModToEntrapment(entrapment, pos, mod.Mod);
            else
                Debugger.Break();
        }

        // Handle non-terminal mods: allow multiple mods at the same position if IdWithMotif is unique
        foreach (var mod in nonTermMods)
        {
            int bestPos = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < entrapment.BaseSequence.Length; i++)
            {
                if (entrapment.BaseSequence[i] != mod.Residue)
                    continue;

                // Only skip if this exact mod is already present at this position
                if (entrapment.OneBasedPossibleLocalizedModifications.TryGetValue(i + 1, out var existingMods) &&
                    existingMods.Any(m2 => m2.IdWithMotif == mod.Mod.IdWithMotif))
                    continue;

                int dist = Math.Abs((i + 1) - mod.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = i + 1;
                }
            }

            if (bestPos > 0)
                AddModToEntrapment(entrapment, bestPos, mod.Mod);
            else
            {
                errors.Add($"Could not assign mod {mod.Mod.IdWithMotif} for entrapment {entrapment.Accession}");
                Debugger.Break();
            }
        }
    }

    internal int FindBestModPosition(IBioPolymer entrapment, IBioPolymer target, ModInfo mod, ref List<string> errors)
    {
        // Handle N-term
        if (mod.Mod.LocationRestriction.Contains("N-term", StringComparison.InvariantCultureIgnoreCase) ||
                                  mod.Mod.LocationRestriction.Contains("5'-", StringComparison.InvariantCultureIgnoreCase))
        {
            // N term mods should be on the first two residues, 1 for n term, second for n term after methionine cleavage
            if (mod.Position > 2)
                Debugger.Break(); // Should not happen

            if (mod.Residue != entrapment.BaseSequence[mod.Position - 1])
            {
                Debugger.Break(); // Should not happen

                // If we get here, swap residues with the nearest matching residue
                var newBaseSeq = entrapment.BaseSequence.ToArray();

                // Find first occurrence of the required residue (excluding N-term)
                int swapIndex = entrapment.BaseSequence.IndexOf(mod.Residue, 1);
                if (swapIndex >= 0)
                {
                    // Swap residues
                    (newBaseSeq[mod.Position - 1], newBaseSeq[swapIndex]) = (newBaseSeq[swapIndex], newBaseSeq[mod.Position - 1]);
                    var baseSequenceProperty = entrapment.GetType().GetProperty("BaseSequence");
                    if (baseSequenceProperty is not null && baseSequenceProperty.CanWrite)
                    {
                        baseSequenceProperty.SetValue(entrapment, new string(newBaseSeq));
                    }
                }
                else 
                    Debugger.Break(); // Should not happen unless mimic mutated away the residue we need

                errors.Add($"Swapped  N-term residues in entrapment {entrapment.Accession} to accommodate modification {mod.Mod.IdWithMotif} at position {mod.Position}");
            }

            return mod.Position; 
        }

        // Handle C-term
        if (mod.Mod.LocationRestriction.Contains("C-term", StringComparison.InvariantCultureIgnoreCase) ||
             mod.Mod.LocationRestriction.Contains("3'-", StringComparison.InvariantCultureIgnoreCase))
        {
            if (mod.Position != entrapment.BaseSequence.Length)
                Debugger.Break(); // Should not happen

            int cTermIndex = entrapment.BaseSequence.Length - 1;
            if (entrapment.BaseSequence[cTermIndex] != mod.Residue)
            {
                // Find first occurrence of the required residue (excluding C-term)
                int swapIndex = entrapment.BaseSequence.LastIndexOf(mod.Residue, cTermIndex, cTermIndex - 2);
                if (swapIndex >= 0)
                {
                    var newBaseSeq = entrapment.BaseSequence.ToArray();
                    // Swap residues
                    (newBaseSeq[cTermIndex], newBaseSeq[swapIndex]) = (newBaseSeq[swapIndex], newBaseSeq[cTermIndex]);

                    // Set the new sequence using reflection as before
                    var baseSequenceProperty = entrapment.GetType().GetProperty("BaseSequence");
                    if (baseSequenceProperty is not null && baseSequenceProperty.CanWrite)
                    {
                        baseSequenceProperty.SetValue(entrapment, new string(newBaseSeq));
                    }
                }
                else // no matching residue found, do nothing
                    Debugger.Break(); // Should not happen unless mimic mutated away the residue we need

                errors.Add($"Swapped C-term residues in entrapment {entrapment.Accession} to accommodate modification {mod.Mod.IdWithMotif} at position {mod.Position}");
            }
            return entrapment.BaseSequence.Length;
        }

        // Find closest matching residue
        int closestPosition = -1;
        int closestDistance = int.MaxValue;
        for (int i = 0; i < entrapment.BaseSequence.Length; i++)
        {
            if (entrapment.BaseSequence[i] != mod.Residue)
                continue;

            if (entrapment.OneBasedPossibleLocalizedModifications.TryGetValue(i + 1, out var existingMods) &&
                existingMods.Any(m => m.IdWithMotif == mod.Mod.IdWithMotif))
                continue;

            int distance = Math.Abs((i + 1) - mod.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPosition = i + 1;
            }
        }
        return closestPosition;
    }

    /// <summary>
    /// Adds a mod to an entrapment protein at the specified position if it is not already present.
    /// </summary>
    internal void AddModToEntrapment(IBioPolymer entrapment, int position, Modification mod)
    {
        if (!entrapment.OneBasedPossibleLocalizedModifications.TryGetValue(position, out var modList))
        {
            modList = [];
            entrapment.OneBasedPossibleLocalizedModifications[position] = modList;
        }
        if (modList.All(m => m.IdWithMotif != mod.IdWithMotif))
            modList.Add(mod);
        else
            Debugger.Break(); // Should not happen if FindBestModPosition is correct
    }
}
