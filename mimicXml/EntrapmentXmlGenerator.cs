using System.Diagnostics;
using Core.Services;
using Core.Services.Entrapment;
using Core.Services.IO;
using Easy.Common.Extensions;
using Omics;
using Omics.Digestion;
using Omics.Modifications;

namespace MimicXml;

public class EntrapmentXmlGenerator(IEntrapmentLoadingService loadingService, IBioPolymerDbWriter writingService, IEntrapmentGroupHistogramService histogramService) : BaseService
{
    internal struct ModInfo
    {
        public char Residue { get; set; }
        public int Position { get; set; }
        public Modification Mod { get; set; }
    }

    public string GetOutputPath(string entrapmentFastaPath) 
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
        loadingService.Verbose = Verbose;
        writingService.Verbose = Verbose;

        ValidateInputPaths(startingXmlPath, entrapmentFastaPath);

        var groups = loadingService.LoadAndParseProteins(new List<string> { startingXmlPath, entrapmentFastaPath });
        foreach (var cluster in groups)
        {
            // Transfer all modifications. 
            var mods = ExtractTargetModifications(cluster.Target.BioPolymer)
                .OrderBy(p => p.Position)
                .ToList();

            foreach (var entrapment in cluster.Entrapments.Select(e => e.BioPolymer))
            {
                foreach (var mod in mods)
                {
                    int position = FindBestModPosition(entrapment, cluster.Target.BioPolymer, mod);
                    if (position > 0)
                        AddModToEntrapment(entrapment, position, mod.Mod);
                    else
                        Debugger.Break(); // No matching residue found
                }
            }

            // Transfer truncations. 
            if (cluster.Target.BioPolymer.TruncationProducts.IsNotNullOrEmpty())
                foreach (var entrapment in cluster.Entrapments.Select(e => e.BioPolymer))
                    entrapment.TruncationProducts.AddRange(cluster.Target.BioPolymer.TruncationProducts);
        }

        var toWrite = groups.SelectMany(g => g.Entrapments.Select(e => e.BioPolymer)).ToList();
        outPath ??= GetOutputPath(startingXmlPath);
        writingService.Write(toWrite, outPath);

        // Print entrapment modification distribution for each entrapment fold
        if (writeModHist)
            histogramService.WriteModificationHistogram(groups, Path.GetDirectoryName(outPath) ?? "", Path.GetFileNameWithoutExtension(startingXmlPath));

        // Print Digestion Counts
        if (writeDigHist && digParams != null)
            histogramService.WriteDigestionHistogram(groups, Path.GetDirectoryName(outPath) ?? "", digParams, Path.GetFileNameWithoutExtension(startingXmlPath));
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
                yield return new ModInfo
                {
                    Residue = position > 0 && position <= target.BaseSequence.Length
                        ? target.BaseSequence[position - 1]
                        : '\0',
                    Position = position,
                    Mod = mod
                };
            }
        }
    }

    internal int FindBestModPosition(IBioPolymer entrapment, IBioPolymer target, ModInfo mod)
    {
        // Handle N-term
        if (mod.Mod.LocationRestriction.Contains("N-term", StringComparison.InvariantCultureIgnoreCase) ||
                                  mod.Mod.LocationRestriction.Contains("5'-", StringComparison.InvariantCultureIgnoreCase))
        {
            // Create new base sequence with N-term residue matching target
            var newBaseSeq = entrapment.BaseSequence.ToArray();

            // TODO: This will not work for RNA due to the M being hard coded. 

            // if target starts with M and entrapment does not, swap first residue with first M in entrapment
            if (target.BaseSequence.StartsWith('M') && !entrapment.BaseSequence.StartsWith('M'))
            {
                char firstResidue = entrapment.BaseSequence[0];
                int indexOfFistM = entrapment.BaseSequence.IndexOf('M', 1, entrapment.BaseSequence.Length - 2);  // -2 to avoid C-term

                newBaseSeq[indexOfFistM] = firstResidue;
                newBaseSeq[0] = 'M';
            }

            // N-term mod on second position due to only occuring after methionine cleavage
            // If the residue at position 2 does not match the mod residue, swap it 
            if (mod.Position == 2 && entrapment.BaseSequence[mod.Position - 1] != mod.Residue) // N-term mod that happens after Methionine cleavage. 
            {
                char secondResidue = entrapment.BaseSequence[1];
                int indexOfFirstModResidue = entrapment.BaseSequence.IndexOf(mod.Residue, 2);
                newBaseSeq[indexOfFirstModResidue] = secondResidue;
                newBaseSeq[1] = mod.Residue;
            }
            else if (mod.Position > 2)
                Debugger.Break(); // Should not happen

            if (newBaseSeq == entrapment.BaseSequence.ToArray())
                return mod.Position; // No change needed

            // With the following code using reflection to set the read-only property:
            var baseSequenceProperty = entrapment.GetType().GetProperty("BaseSequence");
            if (baseSequenceProperty is not null && baseSequenceProperty.CanWrite)
            {
                baseSequenceProperty.SetValue(entrapment, new string(newBaseSeq));
            }
            else
            {
                // Use reflection to set the backing field if property is read-only
                var backingField = entrapment.GetType().GetField("<BaseSequence>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (backingField is not null)
                {
                    backingField.SetValue(entrapment, new string(newBaseSeq));
                }
                else
                {
                    Debugger.Break(); // Could not set BaseSequence
                }
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
                    else
                    {
                        var backingField = entrapment.GetType().GetField("<BaseSequence>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (backingField is not null)
                        {
                            backingField.SetValue(entrapment, new string(newBaseSeq));
                        }
                        else
                        {
                            Debugger.Break(); // Could not set BaseSequence
                        }
                    }
                }
                else // no matching residue found, do nothing
                    Debugger.Break(); // Should not happen
            }
            return entrapment.BaseSequence.Length + 2;
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

    internal void AddModToEntrapment(IBioPolymer entrapment, int position, Modification mod)
    {
        if (!entrapment.OneBasedPossibleLocalizedModifications.TryGetValue(position, out var modList))
        {
            modList = new List<Modification>();
            entrapment.OneBasedPossibleLocalizedModifications[position] = modList;
        }
        if (modList.All(m => m.IdWithMotif != mod.IdWithMotif))
            modList.Add(mod);
        else
            Debugger.Break(); // Should not happen if FindBestModPosition is correct
    }
}
