using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Easy.Common.Extensions;
using Readers;

namespace Core.Models.Entrapment;

public class FdpResults : ResultFile<ResultRecord>, IResultFile
{
    public string Condition { get; set; }
    public bool Pep { get; set; }
    public bool PsmLevel { get; set; }
    public string SpectralMatchLabel { get; set; }

    public ResultRecord OnePercent;

    public FdpResults(List<ResultRecord> results, bool pep)
    {
        Pep = pep;
        Results = results;
        OnePercent = results.First(p => Math.Abs(p.OriginalQ - 0.01) < 0.0000001);
    }

    public FdpResults() : base()
    {

    }

    public override void LoadResults()
    {
        using var csv = new CsvReader(new StreamReader(FilePath), System.Globalization.CultureInfo.InvariantCulture);

        Results = csv.GetRecords<ResultRecord>().ToList();
        Condition = Path.GetFileName(Path.GetDirectoryName(FilePath)) ?? "Unknown";

        OnePercent = Results.First(p => Math.Abs(p.OriginalQ - 0.01) < 0.0000001);
        if (FilePath.Contains("PSM"))
            PsmLevel = true;
        else if (FilePath.Contains("Peptide") || FilePath.Contains("Proteoform"))
            PsmLevel = false;
        else
            throw new Exception("Could not determine if results are PSM or Proteoform/Peptide level from file name.");

        if (FilePath.Contains("PEP"))
            Pep = true;
        else if (FilePath.Contains("Q"))
            Pep = false;
        else
            throw new Exception("Could not determine if results are PEP or Q-value from file name.");

        if (PsmLevel && SpectralMatchLabel.IsNullOrEmpty())
            SpectralMatchLabel = "PSM";
        else if (!PsmLevel && SpectralMatchLabel.IsNullOrEmpty())
            SpectralMatchLabel = "Proteoform";
    }

    public bool OverWriteIndProperties { get; set; } = true;
    public override void WriteResults(string outputPath)
    {
        if (OverWriteIndProperties)
            foreach (var result in Results)
            {
                result.Condition = Condition;
                result.UsePep = Pep;
                if (SpectralMatchLabel != null)
                    result.SpectralMatchLabel = SpectralMatchLabel;
                else
                    result.SpectralMatchLabel = PsmLevel ? "PSM" : "Proteoform";
            }


        using var csv = new CsvWriter(new StreamWriter(outputPath), System.Globalization.CultureInfo.InvariantCulture);

        csv.WriteHeader<ResultRecord>();
        foreach (var record in Results)
        {
            csv.NextRecord();
            csv.WriteRecord(record);
        }
    }

    public override SupportedFileType FileType { get; }
    public override Software Software { get; set; }
}

public static class FdpExtensions
{
    public static FdpResults Average(this IList<FdpResults> resultsToAverage)
    {
        if (!resultsToAverage.Any())
            throw new ArgumentException("No results to average.");

        var first = resultsToAverage.First();
        var allResults = resultsToAverage.SelectMany(r => r.Results).ToList();
        var averagedResults = allResults
            .GroupBy(r => new { r.Condition, r.SpectralMatchLabel, r.UsePep, r.OriginalQ })
            .Select(g => new ResultRecord
            {
                Condition = g.Key.Condition,
                SpectralMatchLabel = g.Key.SpectralMatchLabel,
                UsePep = g.Key.UsePep,
                OriginalQ = g.Key.OriginalQ,
                FdpLowerBound = g.Average(r => r.FdpLowerBound),
                FdpCombined = g.Average(r => r.FdpCombined),
                FdpPaired = g.Average(r => r.FdpPaired),
                TargetCount = (int)Math.Round(g.Average(r => r.TargetCount)),
                EntrapmentCount = (int)Math.Round(g.Average(r => r.EntrapmentCount)),
            })
            .OrderBy(r => r.OriginalQ)
            .ToList();

        var averagedFdpResults = new FdpResults(averagedResults, first.Pep)
        {
            Condition = first.Condition,
            PsmLevel = first.PsmLevel,
            SpectralMatchLabel = first.SpectralMatchLabel,
            FilePath = null
        };
        return averagedFdpResults;
    }
}


public class ResultRecord : IEquatable<ResultRecord>
{
    public string Condition { get; set; } = string.Empty;
    [Optional] public string? FilePath { get; set; }
    public string SpectralMatchLabel { get; set; } = string.Empty;
    public bool UsePep { get; set; }
    public double OriginalQ { get; set; }
    public double FdpLowerBound { get; set; }
    public double FdpCombined { get; set; }
    public double FdpPaired { get; set; }
    public int TargetCount { get; set; }
    public int EntrapmentCount { get; set; }

    public bool Equals(ResultRecord? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Condition == other.Condition && SpectralMatchLabel == other.SpectralMatchLabel && UsePep == other.UsePep && OriginalQ.Equals(other.OriginalQ) && FdpLowerBound.Equals(other.FdpLowerBound) && FdpCombined.Equals(other.FdpCombined) && FdpPaired.Equals(other.FdpPaired) && TargetCount == other.TargetCount && EntrapmentCount == other.EntrapmentCount;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ResultRecord)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Condition);
        hashCode.Add(SpectralMatchLabel);
        hashCode.Add(UsePep);
        hashCode.Add(OriginalQ);
        hashCode.Add(FdpLowerBound);
        hashCode.Add(FdpCombined);
        hashCode.Add(FdpPaired);
        hashCode.Add(TargetCount);
        hashCode.Add(EntrapmentCount);
        return hashCode.ToHashCode();
    }
}