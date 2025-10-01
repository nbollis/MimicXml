using Core.Services.BioPolymer;
using Core.Services.Entrapment;
using Core.Services.IO;
using Core.Services.Mimic;
using Microsoft.Extensions.DependencyInjection;
using Omics;

namespace mimicXml;
public static class AppHost
{
    public static IServiceProvider Services { get; set; } = null!;

    /// <summary>
    /// Creates the base services for the application.
    /// </summary>
    /// <param name="jsonPath">JSON containing configuration for the application.</param>
    /// <returns></returns>
    public static IServiceCollection CreateBaseServices(string jsonPath)
    {
        var services = new ServiceCollection();

        // Register file type detection
        services.AddSingleton<IFileTypeDetectionService, FileTypeDetectionService>();

        // Register individual readers
        services.AddSingleton<FastaProteinDbReader>();
        services.AddSingleton<XmlProteinDbReader>();
        services.AddSingleton<FastaRnaDbReader>();
        services.AddSingleton<XmlRnaDbReader>();

        // Register the composite reader — the app uses this only
        services.AddSingleton<IBioPolymerDbReader<IBioPolymer>>(provider =>
            new CompositeBioPolymerDbReader(
                provider.GetRequiredService<IFileTypeDetectionService>(),
                provider.GetRequiredService<FastaProteinDbReader>(),
                provider.GetRequiredService<XmlProteinDbReader>(),
                provider.GetRequiredService<FastaRnaDbReader>(),
                provider.GetRequiredService<XmlRnaDbReader>()
            ));

        // Register the composite writer
        services.AddSingleton<IBioPolymerDbWriter>(provider =>
            new CompositeBioPolymerDbWriter(
                provider.GetRequiredService<IFileTypeDetectionService>()
            ));

        // Register BioPolymer services
        services.AddSingleton<IDigestionParamsProvider, DigestionParamsProvider>();
        services.AddSingleton<IDigestionHistogramCalculator, DigestionHistogramCalculator>();
        services.AddSingleton<IModificationHistogramCalculator, ModificationHistogramCalculator>();

        // Register EntrapmentEvaluator services
        services.AddSingleton<IEntrapmentLoadingService>(provider =>
            new EntrapmentLoadingService(provider.GetRequiredService<IBioPolymerDbReader<IBioPolymer>>()));
        services.AddSingleton<IEntrapmentGroupHistogramService>(provider =>
            new EntrapmentGroupHistogramService(
                provider.GetRequiredService<IModificationHistogramCalculator>(),
                provider.GetRequiredService<IDigestionHistogramCalculator>()));
        services.AddSingleton<EntrapmentXmlGenerator>(provider => new EntrapmentXmlGenerator(
            provider.GetRequiredService<IEntrapmentLoadingService>(),
            provider.GetRequiredService<IBioPolymerDbWriter>(),
            provider.GetRequiredService<IEntrapmentGroupHistogramService>()));

        // Register MimicExeRunner
        services.AddSingleton<IMimicExeRunner, MimicExeRunner>();

        return services;
    }

    public static T GetService<T>() where T : notnull =>
        Services.GetRequiredService<T>();
}