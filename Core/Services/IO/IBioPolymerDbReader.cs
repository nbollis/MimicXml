using System.Text;
using Core.Util;
using MzLibUtil;
using Omics;
using Proteomics;
using Transcriptomics;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Core.Services.IO;

public interface IBioPolymerDbReader<out T> where T : IBioPolymer
{
    IReadOnlyList<T> Load(string filePath, BioPolymerDbReaderOptions options);
    bool Validate(string filePath);
}

public class XmlProteinDbReader : IBioPolymerDbReader<Protein>
{
    public IReadOnlyList<Protein> Load(string filePath, BioPolymerDbReaderOptions options)
    {
        return ProteinDbLoader.LoadProteinXML(
            proteinDbLocation: filePath,
            generateTargets: options.GenerateDecoys,
            decoyType: options.DecoyType,
            allKnownModifications: Modifications.AllProteinModsKnown,
            isContaminant: false,
            modTypesToExclude: null,
            unknownModifications: out _,
            addTruncations: options.AddTruncations,
            maxHeterozygousVariants: options.MaxHeterozygousVariants,
            minAlleleDepth: options.MinAlleleDepth,
            decoyIdentifier: options.DecoyIdentifier);
    }

    public bool Validate(string filePath)
    {
        // Optionally check against XML schema.
        return true;
    }
}

public class XmlRnaDbReader : IBioPolymerDbReader<RNA>
{
    public IReadOnlyList<RNA> Load(string filePath, BioPolymerDbReaderOptions options)
    {
        return RnaDbLoader.LoadRnaXML(
            rnaDbLocation: filePath,
            generateTargets: options.GenerateDecoys,
            decoyType: options.DecoyType,
            allKnownModifications: null, //TODO: Expose RNA Mods in Mzlib
            isContaminant: false,
            modTypesToExclude: null,
            unknownModifications: out _,
            maxHeterozygousVariants: options.MaxHeterozygousVariants,
            minAlleleDepth: options.MinAlleleDepth,
            decoyIdentifier: options.DecoyIdentifier);
    }

    public bool Validate(string filePath)
    {
        // Optionally check against XML schema.
        return true;
    }
}

public class FastaProteinDbReader : IBioPolymerDbReader<Protein>
{
    public IReadOnlyList<Protein> Load(string filePath, BioPolymerDbReaderOptions options)
    {
        List<Protein> proteins;
        try
        {
            proteins = ProteinDbLoader.LoadProteinFasta(
                proteinDbLocation: filePath,
                generateTargets: options.GenerateDecoys,
                decoyType: options.DecoyType,
                isContaminant: false,
                errors: out _,
                addTruncations: options.AddTruncations,
                decoyIdentifier: options.DecoyIdentifier);
        }
        catch (MzLibException)
        {
            // Do it ourselves. Header lines for mimic will be like: >mimic|Random_P84243_1|shuffle_1
            proteins = [];
            var lines = System.IO.File.ReadAllLines(filePath);

            var sequenceBuilder = new StringBuilder();
            string? currentAccession = null;
            string? currentName = null;
            foreach (var line in lines)
            {
                if (line.StartsWith(">"))
                {
                    // If there is a previous sequence, add it as a Protein
                    if (sequenceBuilder.Length > 0 && currentAccession != null)
                    {
                        var sequence = sequenceBuilder.ToString();
                        var previousProtein = new Protein(sequence, currentAccession, name: currentName);
                        proteins.Add(previousProtein);
                        sequenceBuilder.Clear();
                    }

                    var header = line[1..].Trim();
                    var parts = header.Split('|');
                    if (parts.Length < 2)
                        throw new Exception($"Invalid FASTA header format: {line}");

                    currentAccession = parts[1];
                    currentName = parts.Length > 2 ? parts[2] : currentAccession;
                }
                else
                {
                    sequenceBuilder.Append(line.Trim());
                }
            }
            // Add the last protein after the loop
            if (sequenceBuilder.Length > 0 && currentAccession != null)
            {
                var sequence = sequenceBuilder.ToString();
                var lastProtein = new Protein(sequence, currentAccession, name: currentName);
                proteins.Add(lastProtein);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading FASTA file {filePath}: {ex.Message}", ex);
        }


        return proteins;
    }

    public bool Validate(string filePath)
    {
        // Could validate header formats or do a dry-run read.
        return true;
    }
}

public class FastaRnaDbReader : IBioPolymerDbReader<RNA>
{
    public IReadOnlyList<RNA> Load(string filePath, BioPolymerDbReaderOptions options)
    {
        return RnaDbLoader.LoadRnaFasta(
            rnaDbLocation: filePath,
            generateTargets: options.GenerateDecoys,
            decoyType: options.DecoyType,
            isContaminant: false,
            errors: out _, 
            decoyIdentifier: options.DecoyIdentifier);
    }

    public bool Validate(string filePath)
    {
        // Could validate header formats or do a dry-run read.
        return true;
    }
}