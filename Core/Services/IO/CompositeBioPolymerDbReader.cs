using Omics;
using Proteomics;
using Transcriptomics;
using UsefulProteomicsDatabases;

namespace Core.Services.IO;

public class CompositeBioPolymerDbReader : BaseService, IBioPolymerDbReader<IBioPolymer>
{
    private readonly IFileTypeDetectionService _fileTypeDetector;
    private readonly IBioPolymerDbReader<Protein> _fastaProteinReader;
    private readonly IBioPolymerDbReader<Protein> _xmlProteinReader;
    private readonly IBioPolymerDbReader<RNA> _fastaRnaReader;
    private readonly IBioPolymerDbReader<RNA> _xmlRnaReader;

    public CompositeBioPolymerDbReader(
        IFileTypeDetectionService fileTypeDetector,
        IBioPolymerDbReader<Protein> fastaProteinReader,
        IBioPolymerDbReader<Protein> xmlProteinReader,
        IBioPolymerDbReader<RNA> fastaRnaReader,
        IBioPolymerDbReader<RNA> xmlRnaReader)
    {
        _fileTypeDetector = fileTypeDetector;
        _fastaProteinReader = fastaProteinReader;
        _xmlProteinReader = xmlProteinReader;
        _fastaRnaReader = fastaRnaReader;
        _xmlRnaReader = xmlRnaReader;
    }

    public static readonly BioPolymerDbReaderOptions DefaultDbReaderOptions
        = new BioPolymerDbReaderOptions
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
        var fileType = _fileTypeDetector.DetectFileType(filePath);

        return fileType switch
        {
            BioPolymerDbFileType.ProteinFasta => _fastaProteinReader.Load(filePath, options),
            BioPolymerDbFileType.ProteinXml => _xmlProteinReader.Load(filePath, options),
            BioPolymerDbFileType.RnaFasta => _fastaRnaReader.Load(filePath, options),
            BioPolymerDbFileType.RnaXml => _xmlRnaReader.Load(filePath, options),
            _ => throw new InvalidOperationException("Unsupported or unknown file type."),
        };
    }

    public bool Validate(string filePath)
    {
        var fileType = _fileTypeDetector.DetectFileType(filePath);

        return fileType switch
        {
            BioPolymerDbFileType.ProteinFasta => _fastaProteinReader.Validate(filePath),
            BioPolymerDbFileType.ProteinXml => _xmlProteinReader.Validate(filePath),
            BioPolymerDbFileType.RnaFasta => _fastaRnaReader.Validate(filePath),
            BioPolymerDbFileType.RnaXml => _xmlRnaReader.Validate(filePath),
            _ => false,
        };
    }
}