using Core.Util;
using Omics;
using Omics.Modifications;
using Proteomics;
using Transcriptomics;
using UsefulProteomicsDatabases;

namespace Core.Services.IO;

public class CompositeBioPolymerDbWriter : BaseService, IBioPolymerDbWriter
{
    private readonly IFileTypeDetectionService _fileTypeDetector;

    public CompositeBioPolymerDbWriter(
        IFileTypeDetectionService fileTypeDetector)
    {
        _fileTypeDetector = fileTypeDetector;
    }

    public void Write(List<IBioPolymer> bioPolymers, string outputPath,
        Dictionary<string, HashSet<Tuple<int, Modification>>>? additionalModsToAdd = null)
    {
        if (bioPolymers == null || !bioPolymers.Any())
            throw new ArgumentException("No biopolymers provided.");

        var fileType = _fileTypeDetector.DetectFileType(bioPolymers, outputPath);

        switch (fileType)
        {
            case BioPolymerDbFileType.ProteinFasta:
                ProteinDbWriter.WriteFastaDatabase(bioPolymers.Cast<Protein>().ToList(), outputPath, " ");
                break;

            case BioPolymerDbFileType.ProteinXml:
                ProteinDbWriter.WriteXmlDatabase(additionalModsToAdd, bioPolymers.Cast<Protein>().ToList(), outputPath);
                break;

            case BioPolymerDbFileType.RnaFasta:
                // TODO: You’ll need a RnaDbWriter.WriteFastaDatabase
                throw new NotImplementedException("RNA Fasta writing not implemented");
                //RnaDbWriter.WriteFastaDatabase(bioPolymers.Cast<RNA>().ToList(), outputPath);
                break;

            case BioPolymerDbFileType.RnaXml:
                ProteinDbWriter.WriteXmlDatabase(additionalModsToAdd, bioPolymers.Cast<RNA>().ToList(), outputPath);
                break;

            default:
                throw new InvalidOperationException($"Unsupported file type for writing: {fileType}");
        }

        if (Verbose)
            Logger.WriteLine($"Wrote {bioPolymers.Count} biopolymers to {outputPath}", 1);
    }
}