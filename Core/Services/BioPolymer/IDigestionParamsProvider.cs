using Core.Services.IO;
using Omics.Digestion;
using Proteomics.ProteolyticDigestion;
using Transcriptomics.Digestion;

namespace Core.Services.BioPolymer;

public interface IDigestionParamsProvider
{
    IDigestionParams GetParams(bool isTopDown, bool isRna);
    IDigestionParams GetParams(BioPolymerDbFileType type, bool isTopDown)
    {
        return type switch
        {
            BioPolymerDbFileType.ProteinFasta => GetParams(isTopDown, isRna: false),
            BioPolymerDbFileType.ProteinXml => GetParams(isTopDown, isRna: false),
            BioPolymerDbFileType.RnaFasta => GetParams(isTopDown, isRna: true),
            BioPolymerDbFileType.RnaXml => GetParams(isTopDown, isRna: true),
            _ => throw new InvalidOperationException("Unsupported or unknown file type."),
        };
    }
}

public class DigestionParamsProvider : IDigestionParamsProvider
{
    public IDigestionParams GetParams(bool isTopDown, bool isRna)
    {
        // Return the correct params for the combination
        if (isTopDown && !isRna)
            return new DigestionParams("top-down", maxModificationIsoforms: 4096, maxModsForPeptides: 3);
        if (!isTopDown && !isRna)
            return new DigestionParams();
        if (isTopDown && isRna)
            return new RnaDigestionParams(maxModificationIsoforms: 4096, maxMods: 3);
        return new RnaDigestionParams("RNase T1", 2);
    }
}