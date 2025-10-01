using CommandLine;
using CommandLine.Text;
using Core.Services.BioPolymer;
using Core.Services.IO;
using Core.Services.Mimic;
using Core.Util;
using Microsoft.Extensions.DependencyInjection;
using Omics;
using Omics.Digestion;

namespace MimicXml;

public class Program
{
    static int Main(string[] args)
    {
        // an error code of 0 is returned if the program ran successfully.
        // otherwise, an error code of >0 is returned.
        // this makes it easier to determine via scripts when the program fails.
        int errorCode = 0;

        var parser = new Parser(with => with.HelpWriter = null);
        var parserResult = parser.ParseArguments<CommandLineSettings>(args);

        parserResult
            .WithParsed<CommandLineSettings>(options => errorCode = Run(options))
            .WithNotParsed(errs => errorCode = DisplayHelp(parserResult, errs));

        return errorCode;
    }

    private static int Run(CommandLineSettings options)
    {
        options.ValidateCommandLineSettings();
        Logger.WriteLine("Initializing services...");
        var services = AppHost.CreateBaseServices();
        AppHost.Services = services.BuildServiceProvider();

        // Pull services we need
        var fileDetection = AppHost.GetService<IFileTypeDetectionService>();
        var digProvider = AppHost.GetService<IDigestionParamsProvider>();
        var generator = AppHost.GetService<EntrapmentXmlGenerator>();
        var cleanup = AppHost.GetService<ITempFileCleanupService>();
        generator.Verbose = options.Verbose;

        // Determine file type and get digestion params
        var fileType = fileDetection.DetectFileType(options.StartingXmlPath);
        IDigestionParams digParams = digProvider.GetParams(fileType, options.IsTopDown);

        // If entrapment fasta path is null, generate it ourselves. 
        string? tempFastaPath = null;
        string? tempMimicOutputPath = null;
        if (options.EntrapmentFastaPath is null)
        {
            Logger.WriteLine("Generating entrapment fasta...");
            var reader = AppHost.GetService<IBioPolymerDbReader<IBioPolymer>>();
            var writer = AppHost.GetService<IBioPolymerDbWriter>();
            var mimic = AppHost.GetService<IMimicExeRunner>();

            var fileName = Path.GetFileNameWithoutExtension(options.StartingXmlPath);
            var tempPath = Path.GetTempPath();
            tempFastaPath = Path.Combine(tempPath, $"{fileName}_entrapment.fasta");
            tempMimicOutputPath = Path.Combine(tempPath, $"{fileName}_mimic_output.fasta");

            // Convert XML to FASTA
            Logger.WriteLine("Converting target XML to FASTA...", 1);
            var bioPolymers = reader.Load(options.StartingXmlPath, null!).ToList();
            writer.Write(bioPolymers, tempFastaPath);

            // Run mimic
            Logger.WriteLine("Running mimic...", 1);
            options.MimicParams.InputFastaPath = tempFastaPath;
            options.MimicParams.OutputFastaPath = tempMimicOutputPath;
            var res = mimic.RunAsync(options.MimicParams).Result;
            options.EntrapmentFastaPath = res.EntrapmentPath;
        }

        Logger.WriteLine("Generating mimic xml...");
        generator.GenerateXml(options.StartingXmlPath, options.EntrapmentFastaPath, options.GenerateModificationHistogram, options.GenerateDigestionProductHistogram, digParams, options.OutputXmlPath);

        // Cleanup temp files if we created them
        cleanup.CleanUpTempFile(tempFastaPath);
        cleanup.CleanUpTempFile(tempMimicOutputPath);

        return 0;
    }


    private static int DisplayHelp(ParserResult<CommandLineSettings> parserResult, IEnumerable<Error> errs)
    {
        int errorCode = 0;

        var helpText = HelpText.AutoBuild(parserResult, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.Copyright = "";
            return HelpText.DefaultParsingErrorsHandler(parserResult, h);
        }, e => e);

        helpText.MaximumDisplayWidth = 300;

        Logger.WriteLine(helpText);

        if (errs.Any(x => x.Tag != ErrorType.HelpRequestedError))
        {
            errorCode = 1;
        }

        return errorCode;
    }
}