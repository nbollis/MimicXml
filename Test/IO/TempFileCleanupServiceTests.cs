using System;
using System.IO;
using Core.Services.IO;

namespace Test.IO;

[TestFixture]
public class TempFileCleanupServiceTests
{
    private string _tempFilePath;
    private TempFileCleanupService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new TempFileCleanupService();
        _tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(_tempFilePath, "test");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
    }

    [Test]
    public void CleanUpTempFile_DeletesFile_WhenFileExists()
    {
        Assert.That(File.Exists(_tempFilePath), Is.True);
        _service.CleanUpTempFile(_tempFilePath);
        Assert.That(File.Exists(_tempFilePath), Is.False);
    }

    [Test]
    public void CleanUpTempFile_DoesNothing_WhenFileDoesNotExist()
    {
        File.Delete(_tempFilePath);
        Assert.That(File.Exists(_tempFilePath), Is.False);
        // Should not throw
        Assert.DoesNotThrow(() => _service.CleanUpTempFile(_tempFilePath));
    }

    [Test]
    public void CleanUpTempFile_DoesNothing_WhenPathIsNull()
    {
        Assert.DoesNotThrow(() => _service.CleanUpTempFile(null));
    }
}
