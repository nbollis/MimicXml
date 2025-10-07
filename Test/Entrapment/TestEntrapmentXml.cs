using Core.Services.Entrapment;
using Core.Services.IO;
using Microsoft.Extensions.DependencyInjection;
using MimicXml;

namespace Test.Entrapment
{
    public class EntrapmentXml
    {
        public static int DbRatio = 5;
        public static string XmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "3HumanHistones.xml");
        public static string FastaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "3HumanHistones_MimicTopDown.fasta");

        public static string BiggerXmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "ErrantProts.xml");
        public static string BiggerFastaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "ErrantProts.fasta");

        public static Dictionary<int, (string Xml, string Fasta)> TestDbs = new()
        {
            { 1, (XmlPath, FastaPath) },
            { 2, (BiggerXmlPath, BiggerFastaPath) },
            // Add more test dbs here as needed
        };

        [OneTimeSetUp]
        public void Setup()
        {
            var services = AppHost.CreateBaseServices();
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
            var path = EntrapmentXmlGenerator.GetOutputPath(@"foo\bar\3HumanHistones_MimicTopDown.fasta");
            Assert.That(path, Is.EqualTo(@"foo\bar\3HumanHistones_MimicTopDown_Entrapment.xml"));
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        public void ExtractTargetModifications_ReturnsAllMods(int dbsToUse)
        {
            var xml = TestDbs[dbsToUse].Xml;
            var fasta = TestDbs[dbsToUse].Fasta;
            var reader = AppHost.GetService<IEntrapmentLoadingService>();
            var generator = AppHost.GetService<EntrapmentXmlGenerator>();

            var db = reader.LoadAndParseProteins(new List<string> { xml, fasta });
            var target = db.First().Target.BioPolymer;
            var mods = IModificationAssignmentService.ExtractTargetModifications(target);

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
        [TestCase(1)]
        [TestCase(2)]
        public void FindBestModPosition_FindsCorrectResidue(int dbsToUse)
        {
            var xml = TestDbs[dbsToUse].Xml;
            var fasta = TestDbs[dbsToUse].Fasta;
            var reader = AppHost.GetService<IEntrapmentLoadingService>();
            var modAssigner = new ByResidueModificationAssignmentService();

            var db = reader.LoadAndParseProteins(new List<string>(new List<string> { xml, fasta }));
            var group = db.First();
            var target = group.Target.BioPolymer;
            var entrapment = group.Entrapments.First().BioPolymer;
            var mods = IModificationAssignmentService.ExtractTargetModifications(target);

            List<string> errors = new();
            foreach (var mod in mods)
            {
                int pos = modAssigner.FindBestModPosition(entrapment, target, mod, ref errors);
                if (mod.Position == 1)
                    Assert.That(pos, Is.EqualTo(1));
                else
                    Assert.That(pos, Is.GreaterThan(0));
            }
            Assert.That(errors.Count, Is.EqualTo(0), string.Join(Environment.NewLine, errors));
        }

        [Test]
        [TestCase(1, 3, ModificationAssignmentStrategy.ByPosition)]
        [TestCase(1, 3, ModificationAssignmentStrategy.ByResidue)]
        [TestCase(2, 171, ModificationAssignmentStrategy.ByPosition)]
        [TestCase(2, 171, ModificationAssignmentStrategy.ByResidue)]
        public void TestEntrapmentXmlGeneration(int dbsToUse, int expectedTargetCount, ModificationAssignmentStrategy assignmentStrategy)
        {
            var xml = TestDbs[dbsToUse].Xml;
            var fasta = TestDbs[dbsToUse].Fasta;
            var reader = AppHost.GetService<IEntrapmentLoadingService>();
            var modAssignmentServiceFactory = AppHost.GetService<Func<ModificationAssignmentStrategy, IModificationAssignmentService>>();
            var modAssignmentService = modAssignmentServiceFactory(assignmentStrategy);

            var generator = new EntrapmentXmlGenerator(reader,
                AppHost.GetService<IBioPolymerDbWriter>(),
                AppHost.GetService<IEntrapmentGroupHistogramService>(),
                modAssignmentService);

            // Ensure reading works right
            var original = reader.LoadAndParseProteins(new List<string> { xml, fasta });
            var targetCount = original.Count(g => g.Target is { IsTarget: true, IsEntrapment: false });
            var entrapmentCount = original.Sum(g => g.Entrapments.Count);
            Assert.That(targetCount, Is.EqualTo(expectedTargetCount));
            Assert.That(entrapmentCount, Is.EqualTo(targetCount * original.K));

            // Ensure writing works right
            var outputPath = EntrapmentXmlGenerator.GetOutputPath(xml);
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            generator.GenerateXml(xml, fasta, false, false);
            Assert.That(File.Exists(outputPath), Is.True);

            // Ensure the same number of groups and entrapments after write
            var readIn = reader.LoadAndParseProteins(new List<string> { xml, outputPath });
            targetCount = original.Count(g => g.Target is { IsTarget: true, IsEntrapment: false });
            entrapmentCount = original.Sum(g => g.Entrapments.Count);
            Assert.That(targetCount, Is.EqualTo(expectedTargetCount));
            Assert.That(entrapmentCount, Is.EqualTo(targetCount * readIn.K));

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

                    Assert.That(entrapmentModIds.Count, Is.EqualTo(targetModIds.Count));
                }
            }


            File.Delete(outputPath);
        }
    }
}
