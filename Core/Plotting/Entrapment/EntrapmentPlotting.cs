using Core.Models.Entrapment;
using Core.Util;
using Plotly.NET;
using Plotly.NET.CSharp;
using Readers;
using Chart = Plotly.NET.CSharp.Chart;
using GenericChartExtensions = Plotly.NET.GenericChartExtensions;

namespace Core.Plotting.Entrapment;

public static class EntrapmentPlotting
{

    /// <summary>
    /// Creates a line plot with one series per FDR type and points at each (FDR, FDP) pair.
    /// </summary>
    /// <param name="results"></param>
    /// <returns></returns>
    public static GenericChart PlotFdrVsFDP(this FdpResults results)
    {
        // Multiply all values by 100 and convert to int for percent representation
        var xValues = results.Select(r => (r.OriginalQ * 100)).ToArray();
        var lowerYValues = results.Select(r => (r.FdpLowerBound * 100)).ToArray();
        var combinedYValues = results.Select(r => (r.FdpCombined * 100)).ToArray();
        var pairedYValues = results.Select(r => (r.FdpPaired * 100)).ToArray();

        var lowerTrace = Chart.Line<double, double, string>(
            xValues, lowerYValues, Name: "Lower Bound", LineColor: Color.fromKeyword(ColorKeyword.Blue));
        var combinedTrace = Chart.Line<double, double, string>(
            xValues, combinedYValues, Name: "Combined", LineColor: Color.fromKeyword(ColorKeyword.Green));
        var pairedTrace = Chart.Line<double, double, string>(
            xValues, pairedYValues, Name: "Matched", LineColor: Color.fromKeyword(ColorKeyword.Orange));
        var diagonalTrace = Chart.Line<int, int, string>(
            new int[] { 0, 10 },
            new int[] { 0, 10 },
            ShowMarkers: false,
            ShowLegend: false,
            MarkerColor: Color.fromKeyword(ColorKeyword.Black),
            LineDash: StyleParam.DrawingStyle.Dash
        );

        var titleAddition = $"{results.Condition} " + (results.PsmLevel
            ? "PSM"
            : "Proteoform");
        var axisAddition = results.Pep
            ? " (% by PEP Q-Value)"
            : " (% by Q-value)";

        var chart = Chart.Combine(new[] { lowerTrace, combinedTrace, pairedTrace, diagonalTrace })
            .WithTitle($"{titleAddition} FDR vs FDP")
            .WithXAxisStyle(
                Title.init($"FDR {axisAddition}"),
                MinMax: new Microsoft.FSharp.Core.FSharpOption<Tuple<IConvertible, IConvertible>>(
                    Tuple.Create<IConvertible, IConvertible>(0, 5)
                ),
                ShowGrid: true
            )
            .WithYAxisStyle(
                Title.init("FDP (%)"),
                MinMax: new Microsoft.FSharp.Core.FSharpOption<Tuple<IConvertible, IConvertible>>(
                    Tuple.Create<IConvertible, IConvertible>(0, 5)
                ),
                ShowGrid: true
            )
            .WithLegend(PlotlyBase.DefaultLegend16)
            .WithLayout(PlotlyBase.DefaultLayoutLargerText);
        return chart;
    }

    /// <summary>
    /// Creates a line plot of the histogram of scores when broken down by target, decoy, and entrapment.
    /// </summary>
    /// <param name="searchResults"></param>
    /// <returns></returns>
    public static (GenericChart chart, string annotationText) PlotTargetDecoyEntrapmentCurves(this IList<SpectrumMatchFromTsv> searchResults, string title = "", bool showLegend = true)
    {
        Dictionary<int, int> targets = new();
        Dictionary<int, int> decoys = new();
        Dictionary<int, int> entrapments = new();
        Dictionary<int, int> entrapmentDecoys = new();

        int targetCount = 0;
        int decoyCount = 0;
        int entrapmentCount = 0;
        int entrapmentDecoyCount = 0;

        foreach (var groupedPsms in searchResults.Reverse().GroupBy(p => (p.Accession.IsDecoy(), p.Accession.IsEntrapment())))
        {
            var scores = groupedPsms.Select(p => p.Score).ToArray();
            int groupSize = scores.Length;

            if (groupedPsms.Key == (false, false))
                targetCount += groupSize;
            else if (groupedPsms.Key == (true, false))
                decoyCount += groupSize;
            else if (groupedPsms.Key == (false, true))
                entrapmentCount += groupSize;
            else if (groupedPsms.Key == (true, true))
                entrapmentDecoyCount += groupSize;

            foreach (var score in scores)
            {
                int roundedScore = (int)Math.Round(score);
                if (groupedPsms.Key == (false, false))
                {
                    if (targets.ContainsKey(roundedScore))
                        targets[roundedScore]++;
                    else
                        targets[roundedScore] = 1;
                }
                else if (groupedPsms.Key == (true, false))
                {
                    if (decoys.ContainsKey(roundedScore))
                        decoys[roundedScore]++;
                    else
                        decoys[roundedScore] = 1;
                }
                else if (groupedPsms.Key == (false, true))
                {
                    if (entrapments.ContainsKey(roundedScore))
                        entrapments[roundedScore]++;
                    else
                        entrapments[roundedScore] = 1;
                }
                else if (groupedPsms.Key == (true, true))
                {
                    if (entrapmentDecoys.ContainsKey(roundedScore))
                        entrapmentDecoys[roundedScore]++;
                    else
                        entrapmentDecoys[roundedScore] = 1;
                }
            }
        }

        // Annotation text
        string annotationText =
            $"Targets: {targetCount}<br>" +
            $"Decoys: {decoyCount}<br>" +
            $"Entrapment Targets: {entrapmentCount}<br>" +
            $"Entrapment Decoys: {entrapmentDecoyCount}";

        var targetTrace = Chart.Line<int, int, string>(
            targets.Keys.ToArray(),
            targets.Values.ToArray(),
            Name: "Target",
            MarkerColor: Color.fromKeyword(ColorKeyword.Blue),
            ShowLegend: showLegend);
        var decoyTrace = Chart.Line<int, int, string>(
            decoys.Keys.ToArray(),
            decoys.Values.ToArray(),
            Name: "Decoy",
            MarkerColor: Color.fromKeyword(ColorKeyword.Red),
            ShowLegend: showLegend);
        var entrapmentTrace = Chart.Line<int, int, string>(
            entrapments.Keys.ToArray(),
            entrapments.Values.ToArray(),
            Name: "Entrapment Targets",
            MarkerColor: Color.fromKeyword(ColorKeyword.Green),
            ShowLegend: showLegend);
        var entrapmentDecoyTrace = Chart.Line<int, int, string>(
            entrapmentDecoys.Keys.ToArray(),
            entrapmentDecoys.Values.ToArray(),
            Name: "Entrapment Decoy",
            MarkerColor: Color.fromKeyword(ColorKeyword.Orange),
            ShowLegend: showLegend);

        var chart = Chart.Combine(new[] { targetTrace, decoyTrace, entrapmentTrace, entrapmentDecoyTrace })
            .WithTitle($"{title} Target, Decoy, and Entrapment Score Distribution")
            .WithXAxisStyle(
                Title.init("Score"),
                ShowGrid: true
            )
            .WithYAxisStyle(
                Title.init("Count"),
                ShowGrid: true
            )
            .WithLegend(PlotlyBase.DefaultLegend16)
            .WithLayout(PlotlyBase.DefaultLayoutLargerText);
        return (chart, annotationText);
    }
}