using Core.Plotting.Entrapment;
using Core.Services.Entrapment;
using Core.Services.SearchParsing;
using Core.Util;
using Microsoft.Extensions.DependencyInjection;
using mimicXml;
using Plotly.NET;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.Entrapment
{
    public class EntrapmentXml
    {
        public static int DbRatio = 5;
        public static string XmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "3HumanHistones.xml");
        public static string FastaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "3HumanHistones_MimicTopDown.fasta");

        [OneTimeSetUp]
        public void Setup()
        {
            var services = AppHost.CreateBaseServices("appsettings.json");
            AppHost.Services = services.BuildServiceProvider();
        }

        [Test]
        public void ValidateInputPaths_ThrowsOnInvalidExtensions()
        {
            var generator = AppHost.GetService<EntrapmentXmlGenerator>();

            Assert.That(() => EntrapmentXmlGenerator.ValidateInputPaths("notxml.txt", "file.fasta"),
                Throws.ArgumentException.With.Message.Contain("Starting database must be .xml"));

            Assert.That(() => EntrapmentXmlGenerator.ValidateInputPaths(XmlPath, "file.txt"),
                Throws.ArgumentException.With.Message.Contain("Entrapment database must be .fasta or .fa"));
        }

        [Test]
        public void GetOutputPath_ReturnsExpectedXmlName()
        {
            var generator = AppHost.GetService<EntrapmentXmlGenerator>();
            var path = generator.GetOutputPath(@"foo\bar\3HumanHistones_MimicTopDown.fasta");
            Assert.That(path, Is.EqualTo(@"foo\bar\3HumanHistones_MimicTopDown.xml"));
        }

        [Test]
        public void ExtractTargetModifications_ReturnsAllMods()
        {
            var reader = AppHost.GetService<IEntrapmentLoadingService>();
            var generator = AppHost.GetService<EntrapmentXmlGenerator>();

            var db = reader.LoadAndParseProteins(new List<string> { XmlPath, FastaPath });
            var target = db.First().Target.BioPolymer;
            var mods = generator.ExtractTargetModifications(target);

            // Should match the number of modifications in the target
            var expectedCount = target.OneBasedPossibleLocalizedModifications.Sum(kvp => kvp.Value.Count);
            Assert.That(mods.Count, Is.EqualTo(expectedCount));
            foreach (var extractedMod in mods)
            {
                var residue = target.BaseSequence[extractedMod.Position - 1];
                Assert.That(residue, Is.EqualTo(extractedMod.Residue));

                var modList = target.OneBasedPossibleLocalizedModifications[extractedMod.Position];
                Assert.That(modList.Contains(extractedMod.Mod), Is.True);
            }
        }

        [Test]
        public void FindBestModPosition_FindsCorrectResidue()
        {
            var reader = AppHost.GetService<IEntrapmentLoadingService>();
            var generator = AppHost.GetService<EntrapmentXmlGenerator>();

            var db = reader.LoadAndParseProteins(new List<string> { XmlPath, FastaPath });
            var group = db.First();
            var target = group.Target.BioPolymer;
            var entrapment = group.Entrapments.First().BioPolymer;
            var mods = generator.ExtractTargetModifications(target);

            foreach (var mod in mods)
            {
                int pos = generator.FindBestModPosition(entrapment, target, mod);
                if (mod.Position == 1)
                    Assert.That(pos, Is.EqualTo(1));
                else
                    Assert.That(pos, Is.GreaterThan(0));
            }
        }

        [Test]
        public void TestEntrapmentXmlGeneration()
        {
            var reader = AppHost.GetService<IEntrapmentLoadingService>();
            var generator = AppHost.GetService<EntrapmentXmlGenerator>();

            // Ensure reading works right
            var original = reader.LoadAndParseProteins(new List<string> { XmlPath, FastaPath });
            var targetCount = original.Count(g => g.Target is { IsTarget: true, IsEntrapment: false });
            var entrapmentCount = original.Sum(g => g.Entrapments.Count);
            Assert.That(targetCount, Is.EqualTo(3));
            Assert.That(entrapmentCount, Is.EqualTo(targetCount * DbRatio));

            // Ensure writing works right
            var outputPath = generator.GetOutputPath(FastaPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            generator.GenerateXml(XmlPath, FastaPath, false, false);
            Assert.That(File.Exists(outputPath), Is.True);

            // Ensure the same number of groups and entrapments after write
            var readIn = reader.LoadAndParseProteins(new List<string> { XmlPath, outputPath });
            targetCount = original.Count(g => g.Target is { IsTarget: true, IsEntrapment: false });
            entrapmentCount = original.Sum(g => g.Entrapments.Count);
            Assert.That(targetCount, Is.EqualTo(3));
            Assert.That(entrapmentCount, Is.EqualTo(targetCount * DbRatio));

            // Ensure both sets of targets have the same mods. 
            foreach (var group in original)
            {
                var match = readIn.FirstOrDefault(g => g.Target.Accession == group.Target.Accession);
                Assert.That(match, Is.Not.Null);

                var originalMods = group.Target.BioPolymer.OneBasedPossibleLocalizedModifications;
                var newMods = match!.Target.BioPolymer.OneBasedPossibleLocalizedModifications;
                Assert.That(originalMods.Count, Is.EqualTo(newMods.Count));
                foreach (var pos in originalMods.Keys)
                {
                    Assert.That(newMods.ContainsKey(pos), Is.True);
                    var origList = originalMods[pos].Select(m => m.IdWithMotif).OrderBy(s => s).ToList();
                    var newList = newMods[pos].Select(m => m.IdWithMotif).OrderBy(s => s).ToList();
                    Assert.That(origList, Is.EqualTo(newList));
                }
            }

            // Ensure all entrapments have the same mods as their target.
            foreach (var entrapmentGroup in readIn)
            {
                var targetMods = entrapmentGroup.Target.BioPolymer.OneBasedPossibleLocalizedModifications;
                var targetModIds = targetMods
                    .SelectMany(kvp => kvp.Value)
                    .Select(m => m.IdWithMotif)
                    .OrderBy(s => s)
                    .ToList();
                foreach (var entrapment in entrapmentGroup.Entrapments)
                {
                    var entrapmentMods = entrapment.BioPolymer.OneBasedPossibleLocalizedModifications;
                    var entrapmentModIds = entrapmentMods
                        .SelectMany(kvp => kvp.Value)
                        .Select(m => m.IdWithMotif)
                        .OrderBy(s => s)
                        .ToList();

                    Assert.That(entrapmentMods.Count, Is.EqualTo(targetMods.Count));
                    Assert.That(entrapmentModIds, Is.EqualTo(targetModIds));
                }
            }


            File.Delete(outputPath);
        }
    }
}
