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

public class EntrapmentXmlGenerator(IEntrapmentLoadingService loadingService, IBioPolymerDbWriter writingService, IEntrapmentGroupHistogramService histogramService, IModificationAssignmentService modificationAssignmentService) : BaseService
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
            var mods = IModificationAssignmentService.ExtractTargetModifications(cluster.Target.BioPolymer)
                .OrderBy(p => p.Position)
                .ToList();

            foreach (var entrapment in cluster.Entrapments.Select(e => e.BioPolymer))
            {
                modificationAssignmentService.AssignModifications(entrapment, cluster.Target.BioPolymer, mods, ref errors);
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
}
