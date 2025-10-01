namespace Core.Services.SearchParsing;

public class MetaMorpheusIndividualFilesService : BaseService, IMetaMorpheusParsingService
{
    public static List<string> PathsToAlwaysExclude = new()
    {
        "02-17-20_jurkat_td_rep2_fract1"
    };

    public Dictionary<string, List<string>> GetRelevantFilePaths(string directoryPath, object[]? parameters = null)
    {
        var filePaths = new Dictionary<string, List<string>>();

        // Find PSM and Proteoform/Peptide files
        var psmFiles = Directory.GetFiles(directoryPath, "*PSMs.psmtsv", SearchOption.AllDirectories)
            .Where(p => !p.Contains("AllPSMs"))
            .Where(p => !PathsToAlwaysExclude.Any(p.Contains))
            .ToList();
        if (psmFiles.Count == 0)
            throw new Exception("No PSM files found in result directory.");
        filePaths["PSM"] = psmFiles;

        var proteoformFiles = Directory.GetFiles(directoryPath, "*Proteoforms.psmtsv", SearchOption.AllDirectories)
            .Where(p => !p.Contains("AllProteoforms"))
            .Where(p => !PathsToAlwaysExclude.Any(p.Contains))
            .ToList();
        if (proteoformFiles.Count > 0)
        {
            filePaths["Proteoform"] = proteoformFiles;
        }
        else
        {
            var peptideFiles = Directory.GetFiles(directoryPath, "*Peptides.psmtsv", SearchOption.AllDirectories)
                .Where(p => !p.Contains("AllPeptides"))
                .Where(p => !PathsToAlwaysExclude.Any(p.Contains))
                .ToList();
            if (peptideFiles.Count == 0)
                throw new Exception("No Proteoform or Peptide files found in result directory.");
            filePaths["Peptide"] = peptideFiles;
        }
        return filePaths;
    }
}