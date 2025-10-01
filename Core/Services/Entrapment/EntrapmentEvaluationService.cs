using Core.Models.Entrapment;
using Core.Util;
using Readers;

namespace Core.Services.Entrapment;


public static class EntrapmentEvaluationService
{
    public static void AssignBestScores(List<SpectrumMatchFromTsv> matches, DatabaseSet set, bool usePep, bool splitAmbiguous)
    {
        // Select score function(lower = better)
         Func<SpectrumMatchFromTsv, double> scoreFunc = usePep
             ? m => m.PEP_QValue
             : m => m.QValue;

        // --- Precompute BestScore for every protein/entrapment
        foreach (var match in matches)
        {
            if (match.Accession.IsDecoy())
                continue;

            double score = scoreFunc(match);

            // Use Span for efficient splitting (C# 8+)
            ReadOnlySpan<char> accessionSpan = match.Accession.AsSpan();
            int start = 0;
            while (start < accessionSpan.Length)
            {
                int sep = accessionSpan[start..].IndexOf('|');
                int len = sep == -1 ? accessionSpan.Length - start : sep;
                var accession = accessionSpan.Slice(start, len).ToString();

                var group = set.GetGroupForAccession(accession);
                if (group != null && group.TryGetRecord(accession, out var rec) && score < rec!.BestScore)
                {
                    rec.BestScore = score;
                }

                if (!splitAmbiguous)
                    break; // only assign to first

                start += len + 1;
            }
        }
    }

    public static FdpResults CalculateFdpResults(DatabaseSet dbSet, List<SpectrumMatchFromTsv> matches, bool usePep, int seed, bool splitAmbiguous)
    {
        var results = new List<ResultRecord>();
        var rnd = new Random(seed);

        // Select score function (lower = better)
        Func<SpectrumMatchFromTsv, double> scoreFunc = usePep
            ? m => m.PEP_QValue
            : m => m.QValue;

        // Extract targets and entrapments (T and D) then order ascending (best to worst)
        var ordered = matches
            .Where(m => !m.Accession.IsDecoy())
            .OrderBy(scoreFunc)
            .ToList();

        // --- Iterate α thresholds
        int idx = 0;
        var discoveries = new List<SpectrumMatchFromTsv>();
        var piDelta = new List<BioPolymerRecord>();

        for (double alpha = 0.0; alpha <= 0.100 + 1e-9; alpha += 0.001)
        {
            // Incrementally add discoveries up to α
            while (idx < ordered.Count && scoreFunc(ordered[idx]) <= alpha)
            {
                discoveries.Add(ordered[idx]);
                idx++;
            }

            int NO = discoveries.Count(m => !m.Accession.IsEntrapment());
            int NE = discoveries.Count(m => m.Accession.IsEntrapment());
            int Kalpha = NO + NE;

            if (Kalpha == 0)
            {
                results.Add(new ResultRecord
                {
                    OriginalQ = alpha,
                    FdpLowerBound = 0,
                    FdpCombined = 0,
                    FdpPaired = 0
                });
                continue;
            }

            // --- Lower Bound (Eq. S1)
            double fdpLower = (double)NE / Kalpha;

            // --- Combined (Eq. S2) with r = K
            double r = dbSet.K > 0 ? dbSet.K : 1;
            double fdpCombined = (double)NE * (1.0 + 1.0 / r) / Kalpha;

            // --- Paired/Matched (Eq. S3)
            int k = dbSet.K > 0 ? dbSet.K : 1;
            int vt = 0;

            foreach (var group in dbSet.GetThoseWithResultBelowCutoff(alpha))
            {
                // Construct Πδ = {target, entrapments}
                piDelta.Clear();
                piDelta.AddRange(group.GetSortedPiDelta(rnd, alpha));

                int l = piDelta.Count(p => p.BestScore <= alpha);
                if (l == 0)
                    continue;

                // Find rank of target. 
                int rank = piDelta.IndexOf(group.Target) + 1; // 1-based
                if (rank == k + 1) // target ranked last
                    vt += l; // contributes l·Nl
            }

            double fdpMatched = (double)(NE + vt) / Kalpha;

            results.Add(new ResultRecord
            {
                OriginalQ = alpha,
                FdpLowerBound = fdpLower,
                FdpCombined = fdpCombined,
                FdpPaired = fdpMatched,
                TargetCount = NO,
                EntrapmentCount = NE
            });
        }

        return new FdpResults(results, usePep);
    }
}