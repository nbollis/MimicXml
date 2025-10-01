using Core.Services.IO;

namespace Test.IO;

[TestFixture]
public class FileTypeDetectionServiceTests
{
    private static string ProteinFastaPath => ProteinDbReaderTests.ProteinFastaPath;
    private static string ProteinXmlPath => ProteinDbReaderTests.ProteinXmlPath;
    private static string RnaFastaPath => ProteinDbReaderTests.RnaFastaPath;
    private static string RnaXmlPath => ProteinDbReaderTests.RnaXmlPath;

    private static BioPolymerDbReaderOptions DefaultOptions => ProteinDbReaderTests.DefaultOptions;

    [Test]
    public void FileTypeDetectionService_Detects_ProteinFasta()
    {
        var service = new FileTypeDetectionService();
        var type = service.DetectFileType(ProteinFastaPath);
        Assert.That(type, Is.EqualTo(BioPolymerDbFileType.ProteinFasta));
    }

    [Test]
    public void FileTypeDetectionService_Detects_ProteinXml()
    {
        var service = new FileTypeDetectionService();
        var type = service.DetectFileType(ProteinXmlPath);
        Assert.That(type, Is.EqualTo(BioPolymerDbFileType.ProteinXml));
    }

    [Test]
    public void FileTypeDetectionService_Detects_RnaFasta()
    {
        var service = new FileTypeDetectionService();
        var type = service.DetectFileType(RnaFastaPath);
        Assert.That(type, Is.EqualTo(BioPolymerDbFileType.RnaFasta));
    }

    [Test]
    public void FileTypeDetectionService_Detects_RnaXml()
    {
        var service = new FileTypeDetectionService();
        var type = service.DetectFileType(RnaXmlPath);
        Assert.That(type, Is.EqualTo(BioPolymerDbFileType.RnaXml));
    }
}