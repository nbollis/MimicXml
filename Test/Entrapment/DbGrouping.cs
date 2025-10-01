using Core.Services.Entrapment;
using Microsoft.Extensions.DependencyInjection;
using mimicXml;

namespace Test.Entrapment;

[TestFixture]
public class DbGrouping
{
    public string TargetDb => Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "TwoHumanHistone.fasta");
    public string EntrapmentDb => Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "TwoHumanHistone_BuMimic.fasta");


    [OneTimeSetUp]
    public void Setup()
    {
        var services = AppHost.CreateBaseServices("appsettings.json");
        AppHost.Services = services.BuildServiceProvider();
    }

    [Test]
    public void TestGrouping()
    {
        var loader = AppHost.GetService<IEntrapmentLoadingService>();
        var groups = loader.LoadAndParseProteins(new List<string> { TargetDb, EntrapmentDb });

        Assert.That(groups.Count(), Is.EqualTo(2));
        foreach (var group in groups)
        {
            Assert.That(group.Entrapments.Count, Is.EqualTo(3));
            Assert.That(group.Target.IsTarget, Is.True);
            Assert.That(group.Target.IsEntrapment, Is.False);

            foreach (var entrapment in group.Entrapments)
            {
                Assert.That(entrapment.IsTarget, Is.True);
                Assert.That(entrapment.IsEntrapment, Is.True);
            }
        }
    }
}
