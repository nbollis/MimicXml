using Easy.Common.Extensions;

namespace Core.Services.SearchParsing;

public class MetaMorpheusBulkResultsService : BaseService, IMetaMorpheusParsingService
{
    public Dictionary<string, List<string>> GetRelevantFilePaths(string directoryPath, object[]? parameters = null)
    {
        var filePaths = new Dictionary<string, List<string>>();
        // Find PSM and Proteoform/Peptide files
        var psmFile = Directory.GetFiles(directoryPath, "*AllPSMs.psmtsv", SearchOption.AllDirectories).ToList();
        if (psmFile.Count > 1)
            throw new Exception("Multiple AllPSMs files found in result directory.");
        if (psmFile == null)
            throw new Exception("No PSM file found in result directory.");
        filePaths["PSM"] = psmFile;

        var proteoformFile = Directory.GetFiles(directoryPath, "*AllProteoforms.psmtsv", SearchOption.AllDirectories).ToList();
        if (proteoformFile.IsNotNullOrEmpty())
        {
            if (proteoformFile.Count > 1)
                throw new Exception("Multiple AllProteoforms files found in result directory.");
            filePaths["Proteoform"] = proteoformFile;
        }
        else
        {
            var peptideFile = Directory.GetFiles(directoryPath, "*AllPeptides.psmtsv", SearchOption.AllDirectories).ToList();
            if (peptideFile.Count > 1)
                throw new Exception("Multiple AllPeptides files found in result directory.");
            if (peptideFile == null)
                throw new Exception("No Proteoform or Peptide file found in result directory.");
            filePaths["Peptide"] = peptideFile;
        }
        return filePaths;
    }
}