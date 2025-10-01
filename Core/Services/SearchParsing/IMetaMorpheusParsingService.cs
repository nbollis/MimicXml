namespace Core.Services.SearchParsing;

public interface IMetaMorpheusParsingService
{
    Dictionary<string, List<string>> GetRelevantFilePaths(string directoryPath, object[]? parameters = null);
}