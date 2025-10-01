using CommandLine;

namespace mimicXml;

internal class CommandLineSettings
{
    [Option('x', "targetXml", Required = true, HelpText = "Starting XML file path (.xml or .xml.gz)")]
    public string StartingXmlPath { get; set; }

    [Option('e', "entrapmentFasta", Required = true, HelpText = "Entrapment FASTA file path (.fasta or .fa, can be .gz compressed)")]
    public string EntrapmentFastaPath { get; set; }

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

    public void ValidateCommandLineSettings()
    {
        if (!File.Exists(StartingXmlPath))
            throw new ArgumentException($"Starting XML file does not exist: {StartingXmlPath}");
        if (!File.Exists(EntrapmentFastaPath))
            throw new ArgumentException($"Entrapment FASTA file does not exist: {EntrapmentFastaPath}");

        EntrapmentXmlGenerator.ValidateInputPaths(StartingXmlPath, EntrapmentFastaPath);
    }
}
