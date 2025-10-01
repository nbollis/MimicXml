using Core.Services.BioPolymer;
using Core.Services.IO;

namespace Test.BioPolymer;

[TestFixture]
public class DigestionParamsProviderTests
{
    [Test]
    public void GetParams_TopDownProtein_ReturnsTopDownParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(isTopDown: true, isRna: false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("DigestionParams"));
        Assert.That(result.ToString(), Does.Contain("top-down").IgnoreCase);
    }

    [Test]
    public void GetParams_BottomUpProtein_ReturnsDefaultParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(isTopDown: false, isRna: false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("DigestionParams"));
        Assert.That(result.ToString(), Does.Contain("Trypsin").IgnoreCase);
    }

    [Test]
    public void GetParams_TopDownRna_ReturnsRnaTopDownParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(isTopDown: true, isRna: true);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("RnaDigestionParams"));
        Assert.That(result.DigestionAgent.Name, Does.Contain("top-down"));
    }

    [Test]
    public void GetParams_BottomUpRna_ReturnsRnaDefaultParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(isTopDown: false, isRna: true);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("RnaDigestionParams"));
        Assert.That(result.DigestionAgent.Name, Does.Contain("RNase T1").IgnoreCase);
    }

    [Test]
    public void GetParams_FromFile_TopDownProtein_ReturnsTopDownParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(BioPolymerDbFileType.ProteinXml, true);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("DigestionParams"));
        Assert.That(result.ToString(), Does.Contain("top-down").IgnoreCase);
    }

    [Test]
    public void GetParams_FromFile_BottomUpProtein_ReturnsDefaultParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(BioPolymerDbFileType.ProteinFasta, false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("DigestionParams"));
        Assert.That(result.ToString(), Does.Contain("Trypsin").IgnoreCase);
    }

    [Test]
    public void GetParams_FromFile_TopDownRna_ReturnsRnaTopDownParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(BioPolymerDbFileType.RnaFasta, true);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("RnaDigestionParams"));
        Assert.That(result.DigestionAgent.Name, Does.Contain("top-down"));
    }

    [Test]
    public void GetParams_FromFile_BottomUpRna_ReturnsRnaDefaultParams()
    {
        IDigestionParamsProvider provider = new DigestionParamsProvider();
        var result = provider.GetParams(BioPolymerDbFileType.RnaXml, false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Is.EqualTo("RnaDigestionParams"));
        Assert.That(result.DigestionAgent.Name, Does.Contain("RNase T1").IgnoreCase);
    }
}