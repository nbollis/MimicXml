using Core.Services.IO;
using Proteomics;
using Transcriptomics;

namespace Test.IO;

[TestFixture]
public class CompositeBioPolymerDbReaderTests
{
    private static string ProteinFastaPath => ProteinDbReaderTests.ProteinFastaPath;
    private static string ProteinXmlPath => ProteinDbReaderTests.ProteinXmlPath;
    private static string RnaFastaPath => ProteinDbReaderTests.RnaFastaPath;
    private static string RnaXmlPath => ProteinDbReaderTests.RnaXmlPath;

    private static BioPolymerDbReaderOptions DefaultOptions => ProteinDbReaderTests.DefaultOptions;

    private static CompositeBioPolymerDbReader CreateCompositeReader()
    {
        return new CompositeBioPolymerDbReader(
            new FileTypeDetectionService(),
            new FastaProteinDbReader(),
            new XmlProteinDbReader(),
            new FastaRnaDbReader(),
            new XmlRnaDbReader());
    }

    // Protein FASTA
    [Test]
    public void CompositeBioPolymerDbReader_Validate_ProteinFasta_ReturnsTrue()
    {
        var reader = CreateCompositeReader();
        Assert.That(reader.Validate(ProteinFastaPath), Is.True);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_ProteinFasta_ReturnsProteins()
    {
        var reader = CreateCompositeReader();
        var proteins = reader.Load(ProteinFastaPath, DefaultOptions);
        Assert.That(proteins, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_ProteinFasta_ContainsExpectedProtein()
    {
        var reader = CreateCompositeReader();
        var proteins = reader.Load(ProteinFastaPath, DefaultOptions);
        var aifm1 = proteins.OfType<Protein>().FirstOrDefault(p => p.Name.Contains("AIFM1_MOUSE"));
        Assert.That(aifm1, Is.Not.Null);
        Assert.That(aifm1.BaseSequence.StartsWith("MFRCGGLAGAFKQKLVPLVRTVYVQRPKQRNRLPGNLFQQWRVPLELQMARQMASSGSSG"));
    }

    // Protein XML
    [Test]
    public void CompositeBioPolymerDbReader_Validate_ProteinXml_ReturnsTrue()
    {
        var reader = CreateCompositeReader();
        Assert.That(reader.Validate(ProteinXmlPath), Is.True);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_ProteinXml_ReturnsProteins()
    {
        var reader = CreateCompositeReader();
        var proteins = reader.Load(ProteinXmlPath, DefaultOptions);
        Assert.That(proteins, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_ProteinXml_ContainsExpectedProtein()
    {
        var reader = CreateCompositeReader();
        var proteins = reader.Load(ProteinXmlPath, DefaultOptions);
        var albumin = proteins.OfType<Protein>().FirstOrDefault(p => p.Accession == "P02769");
        Assert.That(albumin, Is.Not.Null);
        Assert.That(albumin.Name, Is.EqualTo("ALBU_BOVIN"));
        Assert.That(albumin.BaseSequence.Length, Is.EqualTo(607));
        Assert.That(albumin.BaseSequence.StartsWith("MKWVTFISLL"));
    }

    // RNA FASTA
    [Test]
    public void CompositeBioPolymerDbReader_Validate_RnaFasta_ReturnsTrue()
    {
        var reader = CreateCompositeReader();
        Assert.That(reader.Validate(RnaFastaPath), Is.True);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_RnaFasta_ReturnsRnas()
    {
        var reader = CreateCompositeReader();
        var rnas = reader.Load(RnaFastaPath, DefaultOptions);
        Assert.That(rnas, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_RnaFasta_ContainsExpectedRna()
    {
        var reader = CreateCompositeReader();
        var rnas = reader.Load(RnaFastaPath, DefaultOptions);
        var rna = rnas.OfType<RNA>().FirstOrDefault(r => r.BaseSequence.StartsWith("GGGGCUAUAGCUCAGCUGGGAGAGCGCCUGCUUUGCACGCAGGAGGUCUGCGGUUCGAUCCCGCAUAGCUCCACCA"));
        Assert.That(rna, Is.Not.Null);
        Assert.That(rna.Accession, Is.Not.Empty);
    }

    // RNA XML
    [Test]
    public void CompositeBioPolymerDbReader_Validate_RnaXml_ReturnsTrue()
    {
        var reader = CreateCompositeReader();
        Assert.That(reader.Validate(RnaXmlPath), Is.True);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_RnaXml_ReturnsRnas()
    {
        var reader = CreateCompositeReader();
        var rnas = reader.Load(RnaXmlPath, DefaultOptions);
        Assert.That(rnas, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void CompositeBioPolymerDbReader_Load_RnaXml_ContainsExpectedRna()
    {
        var reader = CreateCompositeReader();
        var rnas = reader.Load(RnaXmlPath, DefaultOptions);
        var rna = rnas.OfType<RNA>().FirstOrDefault(r => r.Accession == "20mer1");
        Assert.That(rna, Is.Not.Null);
        Assert.That(rna.BaseSequence, Is.EqualTo("GUACUGCCUCUAGUGAAGCA"));
    }
}