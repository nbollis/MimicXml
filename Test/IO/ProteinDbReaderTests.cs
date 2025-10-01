using Core.Services.IO;
using UsefulProteomicsDatabases;

namespace Test.IO;

[TestFixture]
public class ProteinDbReaderTests
{
    public static string ProteinFastaPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "IO/TestData/Protein.fasta");
    public static string ProteinXmlPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "IO/TestData/Protein.xml");
    public static string RnaFastaPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "IO/TestData/RNA.fasta");
    public static string RnaXmlPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "IO/TestData/RNA.xml");

    public static BioPolymerDbReaderOptions DefaultOptions => new()
    {
        DecoyType = DecoyType.None,
        AddTruncations = false,
        DecoyIdentifier = "DECOY_",
        MaxHeterozygousVariants = 0,
        MinAlleleDepth = 0,
        ConvertTtoU = false
    };

    // Protein XML
    [Test]
    public void XmlProteinDbReader_Validate_ReturnsTrue()
    {
        var reader = new XmlProteinDbReader();
        Assert.That(reader.Validate(ProteinXmlPath), Is.True);
    }

    [Test]
    public void XmlProteinDbReader_Load_ReturnsProteins()
    {
        var reader = new XmlProteinDbReader();
        var proteins = reader.Load(ProteinXmlPath, DefaultOptions);
        Assert.That(proteins, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void XmlProteinDbReader_Load_ContainsExpectedProtein()
    {
        var reader = new XmlProteinDbReader();
        var proteins = reader.Load(ProteinXmlPath, DefaultOptions);
        var albumin = proteins.FirstOrDefault(p => p.Accession == "P02769");
        Assert.That(albumin, Is.Not.Null);
        Assert.That(albumin.Name, Is.EqualTo("ALBU_BOVIN"));
        Assert.That(albumin.BaseSequence.Length, Is.EqualTo(607));
        Assert.That(albumin.BaseSequence.StartsWith("MKWVTFISLL"));
    }

    // Protein FASTA
    [Test]
    public void FastaProteinDbReader_Validate_ReturnsTrue()
    {
        var reader = new FastaProteinDbReader();
        Assert.That(reader.Validate(ProteinFastaPath), Is.True);
    }

    [Test]
    public void FastaProteinDbReader_Load_ReturnsProteins()
    {
        var reader = new FastaProteinDbReader();
        var proteins = reader.Load(ProteinFastaPath, DefaultOptions);
        Assert.That(proteins, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void FastaProteinDbReader_Load_ContainsExpectedProtein()
    {
        var reader = new FastaProteinDbReader();
        var proteins = reader.Load(ProteinFastaPath, DefaultOptions);
        var aifm1 = proteins.FirstOrDefault(p => p.Name.Contains("AIFM1_MOUSE"));
        Assert.That(aifm1, Is.Not.Null);
        Assert.That(aifm1.BaseSequence.StartsWith("MFRCGGLAGAFKQKLVPLVRTVYVQRPKQRNRLPGNLFQQWRVPLELQMARQMASSGSSG"));
    }

    // RNA XML
    [Test]
    public void XmlRnaDbReader_Validate_ReturnsTrue()
    {
        var reader = new XmlRnaDbReader();
        Assert.That(reader.Validate(RnaXmlPath), Is.True);
    }

    [Test]
    public void XmlRnaDbReader_Load_ReturnsRnas()
    {
        var reader = new XmlRnaDbReader();
        var rnas = reader.Load(RnaXmlPath, DefaultOptions);
        Assert.That(rnas, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void XmlRnaDbReader_Load_ContainsExpectedRna()
    {
        var reader = new XmlRnaDbReader();
        var rnas = reader.Load(RnaXmlPath, DefaultOptions);
        var rna = rnas.FirstOrDefault(r => r.Accession == "20mer1");
        Assert.That(rna, Is.Not.Null);
        Assert.That(rna.BaseSequence, Is.EqualTo("GUACUGCCUCUAGUGAAGCA"));
    }

    // RNA FASTA
    [Test]
    public void FastaRnaDbReader_Validate_ReturnsTrue()
    {
        var reader = new FastaRnaDbReader();
        Assert.That(reader.Validate(RnaFastaPath), Is.True);
    }

    [Test]
    public void FastaRnaDbReader_Load_ReturnsRnas()
    {
        var reader = new FastaRnaDbReader();
        var rnas = reader.Load(RnaFastaPath, DefaultOptions);
        Assert.That(rnas, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void FastaRnaDbReader_Load_ContainsExpectedRna()
    {
        var reader = new FastaRnaDbReader();
        var rnas = reader.Load(RnaFastaPath, DefaultOptions);
        var rna = rnas.FirstOrDefault(r => r.BaseSequence.StartsWith("GGGGCUAUAGCUCAGCUGGGAGAGCGCCUGCUUUGCACGCAGGAGGUCUGCGGUUCGAUCCCGCAUAGCUCCACCA"));
        Assert.That(rna, Is.Not.Null);
        Assert.That(rna.Accession, Is.Not.Empty);
    }
}