using CommandLine;
using Core.Services.Mimic;

namespace mimicXml;

internal class CommandLineSettings
{
    public MimicParams MimicParams = null!;

    [Option('x', "targetXml", Required = true, HelpText = "Starting XML file path (.xml or .xml.gz)")]
    public string StartingXmlPath { get; set; }

    [Option('e', "entrapmentFasta", Required = false, Default = null, HelpText = "[Optional] Entrapment FASTA file path (.fasta or .fa, can be .gz compressed). If this is not set, an entrapment fasta will be generated using mimic and the mimic specifc parameters")]
    public string? EntrapmentFastaPath { get; set; } = null;

    [Option('o', "output", Required = false, Default = null, HelpText = "[Optional] Output XML file path (.xml), if not set output will be to the same location as the entrapment fasta path")]
    public string? OutputXmlPath { get; set; }

    [Option('v', "verbose", Required = false, Default = true, HelpText = "Verbose output to console (default: true)")]
    public bool Verbose { get; set; } = true;

    [Option('m', "modHist", Required = false, Default = true, HelpText = "Generate a histogram of modification frequencies in the entrapment proteins (default: true)")]
    public bool GenerateModificationHistogram { get; set; } = true;

    [Option('d', "digHist", Required = false, Default = true, HelpText = "Generate a histogram of digestion products in the entrapment proteins (default: true)")]
    public bool GenerateDigestionProductHistogram { get; set; } = true;

    [Option('t', "isTopDown", Required = false, Default = true, HelpText = "Generate entrapment proteins for top-down searches (default: true). If false, generates for bottom-up searches.")]
    public bool IsTopDown { get; set; } = true;


    [Option("mimicMultFactor", Required = false, Default = 1, HelpText = "Determines the number of times the database should be multiplied (Default: 1). Higher values create more entrapment sequences, but take longer to run. If no entrapment database is provided, mimic will run using this parameter.")]
    public int MimicMultFactor { get; set; } = 1;

    [Option("mimicRetainTerm", Required = false, Default = 1, HelpText = "The number of terminal residues that will be retained if running in top-down mode (Default: 0). If no entrapment database is provided, mimic will run using this parameter.")]
    public int MimicTerminalResiduesToRetain { get; set; } = 1;

    public void ValidateCommandLineSettings()
    {
        if (!File.Exists(StartingXmlPath))
            throw new ArgumentException($"Starting XML file does not exist: {StartingXmlPath}");
        if (!File.Exists(EntrapmentFastaPath))
            throw new ArgumentException($"Entrapment FASTA file does not exist: {EntrapmentFastaPath}");

        EntrapmentXmlGenerator.ValidateInputPaths(StartingXmlPath, EntrapmentFastaPath);

        MimicParams = new() 
        { 
            MultFactor = MimicMultFactor, 
            TerminalResiduesToRetain = MimicTerminalResiduesToRetain,
            NoDigest = IsTopDown,
        };
    }
}
