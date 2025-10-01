using CommandLine;
using CommandLine.Text;
using Core.Services.BioPolymer;
using Core.Services.IO;
using Core.Util;
using Microsoft.Extensions.DependencyInjection;
using Omics.Digestion;

namespace mimicXml;

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
        var services = AppHost.CreateBaseServices("appsettings.json");
        AppHost.Services = services.BuildServiceProvider();

        var fileDetection = AppHost.GetService<IFileTypeDetectionService>();
        var digProvider = AppHost.GetService<IDigestionParamsProvider>();
        var generator = AppHost.GetService<EntrapmentXmlGenerator>();
        generator.Verbose = options.Verbose;

        var fileType = fileDetection.DetectFileType(options.StartingXmlPath);
        IDigestionParams digParams = digProvider.GetParams(fileType, options.IsTopDown);

        Logger.WriteLine("Generating mimic xml...");
        generator.GenerateXml(options.StartingXmlPath, options.EntrapmentFastaPath, options.GenerateModificationHistogram, options.GenerateDigestionProductHistogram, digParams, options.OutputXmlPath);

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