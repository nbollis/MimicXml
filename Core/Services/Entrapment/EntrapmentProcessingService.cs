using Core.Models.Entrapment;
using Readers;

namespace Core.Services.Entrapment;

public interface IEntrapmentProcessingService : IBaseService
{
    FdpResults Process(DatabaseSet dbSet, string tsvPath, bool usePep, int seed, bool splitAmbiguous);
}

public class EntrapmentProcessingService : BaseService, IEntrapmentProcessingService
{
    public FdpResults Process(DatabaseSet dbSet, string tsvPath, bool usePep, int seed, bool splitAmbiguous)
    {
        var spectralMatches = SpectrumMatchTsvReader.ReadTsv(tsvPath, out _);

        dbSet.Reset();
        EntrapmentEvaluationService.AssignBestScores(spectralMatches, dbSet, usePep, splitAmbiguous);

        // Shared logic for FdpResults
        var results = EntrapmentEvaluationService.CalculateFdpResults(dbSet, spectralMatches, usePep, seed, splitAmbiguous);

        return results;
    }
}