using Core.Models.Entrapment;
using Core.Plotting;
using Core.Plotting.Entrapment;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Chart = Plotly.NET.CSharp.Chart;

namespace Core.Services.Entrapment;

public interface IEntrapmentOutputService : IBaseService
{
    void WriteResultsCsv(FdpResults results, string outputPath, bool individualFiles = false);
    void PlotResults(FdpResults results, string outputPath, bool individualFiles = false);
    void PlotAllResults(IList<FdpResults> results, string outputDirectory, bool individualFiles = false);
    string GetOutputPath(string outputDirectory, string type, bool pep, string extension, bool individualFiles = false)
    {
        return Path.Combine(outputDirectory, $"EntrapmentFDP{(individualFiles ? "_IndAveraged" : "")}_{type}_{(pep ? "PEP" : "Q")}.{extension}");
    }

    void AggregateOnePercentResults(List<string> directoryPaths, string outputDirectory, bool individualFiles = false)
    {
        // Aggregate all
        var allResults = new List<FdpResults>();
        foreach (var dir in directoryPaths)
        {
            string searchKey = individualFiles ? "EntrapmentFDP_IndAveraged*.csv" : "EntrapmentFDP_*.csv";
            var csvFiles = Directory.GetFiles(dir, searchKey);
            foreach (var file in csvFiles)
            {
                var results = new FdpResults { FilePath = file };
                results.LoadResults();
                allResults.Add(results);

                foreach (var record in results)
                {
                    record.Condition = results.Condition;
                    record.UsePep = results.Pep;
                    record.SpectralMatchLabel = results.PsmLevel ? "PSM" : "Proteoform";
                }
            }
        }

        // Write a csv for each distinct type (PSM/Proteoform) and (PEP/Q)
        var grouped = allResults.GroupBy(r => (r.PsmLevel, r.Pep));
        foreach (var group in grouped)
        {
            var typeLabel = group.Key.PsmLevel ? "PSM" : "Proteoform";
            var outputPath = GetOutputPath(outputDirectory, typeLabel, group.Key.Pep, "csv", individualFiles);

            // If file exists, read it in and append unique entries
            if (File.Exists(outputPath))
            {
                var existing = new FdpResults { FilePath = outputPath };
                existing.LoadResults();
                var existingSet = existing.Results;
                var newEntries = group.Select(g => g.OnePercent).Where(r => !existingSet.Contains(r)).ToList();
                if (newEntries.Count == 0)
                    continue;
                existing.Results.AddRange(newEntries);

                existing.OverWriteIndProperties = false;
                existing.WriteResults(outputPath);
                continue;
            }

            // Otherwise, just write new file
            var file = new FdpResults
            {
                Results = group.Select(g => g.OnePercent).ToList(),
                Condition = "All",
                Pep = group.Key.Pep,
                PsmLevel = group.Key.PsmLevel,
                SpectralMatchLabel = typeLabel,
                FilePath = outputPath
            };
            file.OverWriteIndProperties = false;
            file.WriteResults(outputPath);
        }

    }
}

public class EntrapmentOutputService : BaseService, IEntrapmentOutputService
{
    public void WriteResultsCsv(FdpResults results, string outputDirectory, bool individualFiles = false)
    {
        var outputPath = (this as IEntrapmentOutputService).GetOutputPath(outputDirectory, results.SpectralMatchLabel, results.Pep, "csv", individualFiles);

        results.WriteResults(outputPath);
    }

    public void PlotResults(FdpResults results, string outputDirectory, bool individualFiles = false)
    {
        var outputPath = (this as IEntrapmentOutputService).GetOutputPath(outputDirectory, results.SpectralMatchLabel, results.Pep, "png", individualFiles)
            .Replace(".png", "");

        var chart = results.PlotFdrVsFDP();
        //Plotly.NET.GenericChartExtensions.Show(chart);
        chart.SavePNG(outputPath, null, 800, 800);
    }

    public void PlotAllResults(IList<FdpResults> results, string outputDirectory, bool individualFiles = false)
    {
        List<GenericChart> charts = new();
        foreach (var result in results)
        {
            var chart = result.PlotFdrVsFDP();
            charts.Add(chart);
        }

        var titles = results.Select(r => $"{r.Condition} {(r.PsmLevel ? "PSM" : "Proteoform")} {(r.Pep ? "PEP" : "Q")}").ToArray();

        var final = Chart.Grid(charts, 2, 2, titles)
            .WithTitle($"{results.First().Condition} Entrapment FDR vs FDP Summary")
            .WithLegend(PlotlyBase.DefaultLegend16);
        var outputPath = Path.Combine(outputDirectory, $"EntrapmentFDP{(individualFiles ? "_IndAveraged" : "")}_Summary");
        final.SavePNG(outputPath, null, 1600, 1600);
    }
}