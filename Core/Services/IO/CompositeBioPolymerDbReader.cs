using Omics;
using Proteomics;
using Transcriptomics;
using UsefulProteomicsDatabases;

namespace Core.Services.IO;

public class CompositeBioPolymerDbReader(
    IFileTypeDetectionService fileTypeDetector,
    IBioPolymerDbReader<Protein> fastaProteinReader,
    IBioPolymerDbReader<Protein> xmlProteinReader,
    IBioPolymerDbReader<RNA> fastaRnaReader,
    IBioPolymerDbReader<RNA> xmlRnaReader)
    : BaseService, IBioPolymerDbReader<IBioPolymer>
{
    public static readonly BioPolymerDbReaderOptions DefaultDbReaderOptions
        = new()
        {
            DecoyType = DecoyType.None,
            DecoyIdentifier = "DECOY",
            AddTruncations = false,
            MaxHeterozygousVariants = 0,
            MinAlleleDepth = 0
        };

    public IReadOnlyList<IBioPolymer> Load(string filePath, BioPolymerDbReaderOptions options = null!)
    {
        options ??= DefaultDbReaderOptions;
        var fileType = fileTypeDetector.DetectFileType(filePath);

        return fileType switch
        {
            BioPolymerDbFileType.ProteinFasta => fastaProteinReader.Load(filePath, options),
            BioPolymerDbFileType.ProteinXml => xmlProteinReader.Load(filePath, options),
            BioPolymerDbFileType.RnaFasta => fastaRnaReader.Load(filePath, options),
            BioPolymerDbFileType.RnaXml => xmlRnaReader.Load(filePath, options),
            _ => throw new InvalidOperationException("Unsupported or unknown file type."),
        };
    }

    public bool Validate(string filePath)
    {
        var fileType = fileTypeDetector.DetectFileType(filePath);

        return fileType switch
        {
            BioPolymerDbFileType.ProteinFasta => fastaProteinReader.Validate(filePath),
            BioPolymerDbFileType.ProteinXml => xmlProteinReader.Validate(filePath),
            BioPolymerDbFileType.RnaFasta => fastaRnaReader.Validate(filePath),
            BioPolymerDbFileType.RnaXml => xmlRnaReader.Validate(filePath),
            _ => false,
        };
    }
}