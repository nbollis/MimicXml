using Core.Services.Mimic;

namespace Test.Mimic;

[TestFixture]
public class MimicExeRunnerTests
{
    private string _testInputFasta;
    private string _testOutputFasta;

    [SetUp]
    public void SetUp()
    {
        // Use a small test FASTA file that is included in your test data
        _testInputFasta = Path.Combine(TestContext.CurrentContext.TestDirectory, "IO", "TestData", "Protein.fasta");
        _testOutputFasta = Path.Combine(Path.GetTempPath(), $"mimic_test_{Path.GetRandomFileName()}.fasta");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testOutputFasta))
            File.Delete(_testOutputFasta);
    }

    [Test]
    public async Task RunAsync_CreatesOutputFile_AndReturnsSuccess()
    {
        // Arrange
        var mimicParams = new MimicParams
        {
            InputFastaPath = _testInputFasta,
            OutputFastaPath = _testOutputFasta,
            MultFactor = 1,
            NoDigest = true,
            TerminalResiduesToRetain = 0
        };

        var runner = new MimicExeRunner();

        // Act
        var (exitCode, entrapmentPath) = await runner.RunAsync(mimicParams);

        // Assert
        Assert.That(exitCode, Is.EqualTo(0), "mimic.exe should exit with code 0");
        Assert.That(entrapmentPath, Is.EqualTo(_testOutputFasta));
        Assert.That(File.Exists(_testOutputFasta), Is.True, "Output FASTA file should be created");
        Assert.That(new FileInfo(_testOutputFasta).Length, Is.GreaterThan(0), "Output FASTA file should not be empty");
    }

    [Test]
    public void RunAsync_ThrowsIfInputFileMissing()
    {
        // Arrange
        var mimicParams = new MimicParams
        {
            InputFastaPath = "nonexistent.fasta",
            OutputFastaPath = _testOutputFasta,
            MultFactor = 1,
            NoDigest = true,
            TerminalResiduesToRetain = 0
        };

        var runner = new MimicExeRunner();

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(async () => await runner.RunAsync(mimicParams));
        Assert.That(ex.Message, Does.Contain("Input FASTA"));
    }

    [Test]
    public void Constructor_ThrowsIfExeMissing()
    {
        // Arrange
        var exePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mimic.exe");
        var backupPath = exePath + ".bak";
        if (File.Exists(exePath))
            File.Move(exePath, backupPath);

        try
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => new MimicExeRunner());
        }
        finally
        {
            if (File.Exists(backupPath))
                File.Move(backupPath, exePath);
        }
    }
}