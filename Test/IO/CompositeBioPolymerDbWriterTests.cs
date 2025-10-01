using Core.Services.IO;
using Omics;
using Proteomics;
using Transcriptomics;

namespace Test.IO;

[TestFixture]
public class CompositeBioPolymerDbWriterTests
{
    private static Protein CreateProtein(string name = "P1", string seq = "MSEQUENCE") =>
        new Protein(seq, "ACC1", name: name);

    private static RNA CreateRna(string accession = "RNA1", string seq = "AUGCUU") =>
        new RNA(seq, accession);

    private static string GetTempFilePath(string extension)
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + extension);
    }

    [Test]
    public void Write_ProteinFasta_WritesFile()
    {
        var fileTypeDetector = new FileTypeDetectionService();
        var writer = new CompositeBioPolymerDbWriter(fileTypeDetector);

        var proteins = new List<IBioPolymer> { CreateProtein() };
        var tempFile = GetTempFilePath(".fasta");

        try
        {
            Assert.DoesNotThrow(() => writer.Write(proteins, tempFile));
            Assert.That(File.Exists(tempFile), Is.True);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Test]
    public void Write_ProteinXml_WritesFile()
    {
        var fileTypeDetector = new FileTypeDetectionService();
        var writer = new CompositeBioPolymerDbWriter(fileTypeDetector);

        var proteins = new List<IBioPolymer> { CreateProtein() };
        var tempFile = GetTempFilePath(".xml");

        try
        {
            Assert.DoesNotThrow(() => writer.Write(proteins, tempFile));
            Assert.That(File.Exists(tempFile), Is.True);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Test]
    public void Write_RnaFasta_ThrowsNotImplemented()
    {
        var fileTypeDetector = new FileTypeDetectionService();
        var writer = new CompositeBioPolymerDbWriter(fileTypeDetector);

        var rnas = new List<IBioPolymer> { CreateRna() };
        var tempFile = GetTempFilePath(".fasta");

        try
        {
            Assert.Throws<NotImplementedException>(() => writer.Write(rnas, tempFile));
            Assert.That(File.Exists(tempFile), Is.False);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Test]
    public void Write_RnaXml_WritesFile()
    {
        var fileTypeDetector = new FileTypeDetectionService();
        var writer = new CompositeBioPolymerDbWriter(fileTypeDetector);

        var rnas = new List<IBioPolymer> { CreateRna() };
        var tempFile = GetTempFilePath(".xml");

        try
        {
            Assert.DoesNotThrow(() => writer.Write(rnas, tempFile));
            Assert.That(File.Exists(tempFile), Is.True);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Test]
    public void Write_MixedProteinsAndRnas_Throws()
    {
        var fileTypeDetector = new FileTypeDetectionService();
        var writer = new CompositeBioPolymerDbWriter(fileTypeDetector);

        var mixed = new List<IBioPolymer> { CreateProtein(), CreateRna() };
        var tempFile = GetTempFilePath(".fasta");

        try
        {
            Assert.Throws<InvalidCastException>(() => writer.Write(mixed, tempFile));
            Assert.That(File.Exists(tempFile), Is.False);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Test]
    public void Write_EmptyList_Throws()
    {
        var fileTypeDetector = new FileTypeDetectionService();
        var writer = new CompositeBioPolymerDbWriter(fileTypeDetector);

        var empty = new List<IBioPolymer>();
        var tempFile = GetTempFilePath(".fasta");

        try
        {
            Assert.Throws<ArgumentException>(() => writer.Write(empty, tempFile));
            Assert.That(File.Exists(tempFile), Is.False);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
