using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Models.Entrapment;
using Core.Services.BioPolymer;
using Core.Services.Entrapment;
using Core.Services.IO;
using MimicXml;
using NUnit.Framework;
using Omics;
using Proteomics;

namespace Test.Entrapment
{
    [TestFixture]
    public class HistogramCalculatorTests
    {
        private List<IBioPolymer> _proteins;
        private List<EntrapmentGroup> _groups;

        [SetUp]
        public void SetUp()
        {
            var xmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "3HumanHistones.xml");
            var loader = AppHost.GetService<IBioPolymerDbReader<IBioPolymer>>();
            _proteins = loader.Load(xmlPath, null!).ToList();

            // Each protein as a target, no entrapments (to match the histogram tests)
            var mimicPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Entrapment", "TestData", "3HumanHistones_MimicTopDown.fasta");
            var entrapLoader = AppHost.GetService<IEntrapmentLoadingService>();
            _groups = entrapLoader.LoadAndParseProteins(new List<string> { xmlPath, mimicPath }).ToList();
        }

        [Test]
        public void ModificationHistogramCalculator_ReturnsExpectedHistogram()
        {
            var calculator = new ModificationHistogramCalculator();
            var histogram = calculator.GetModificationHistogram(_proteins);

            Assert.That(histogram, Is.Not.Null);
            Assert.That(histogram.Count, Is.EqualTo(2));
            Assert.That(histogram[5], Is.EqualTo(2));
            Assert.That(histogram[7], Is.EqualTo(1));
        }

        [Test]
        public void DigestionHistogramCalculator_ReturnsExpectedHistogram()
        {
            var calculator = new DigestionHistogramCalculator();
            var digestionParams = AppHost.GetService<IDigestionParamsProvider>()
                .GetParams(true, false);

            var histogram = calculator.GetDigestionHistogram(_proteins, digestionParams);

            Assert.That(histogram, Is.Not.Null);
            Assert.That(histogram.Count, Is.EqualTo(2));
            Assert.That(histogram[67], Is.EqualTo(2));
            Assert.That(histogram[192], Is.EqualTo(1));
        }

        [Test]
        public void EntrapmentGroupHistogramService_WritesModificationHistogramCsv_WithCorrectValues()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var modCalc = new ModificationHistogramCalculator();
            var digCalc = new DigestionHistogramCalculator();
            var service = new EntrapmentGroupHistogramService(modCalc, digCalc);

            string dbName = "testdb";
            service.WriteModificationHistogram(_groups, tempDir, dbName);

            var csvPath = Path.Combine(tempDir, $"{dbName}_ModificationHistogram.csv");
            Assert.That(File.Exists(csvPath), Is.True);

            var lines = File.ReadAllLines(csvPath);
            Assert.That(lines[0], Does.Contain("Modifications,Targets"));

            // Check for correct histogram values
            Assert.That(lines.Any(l => l.StartsWith("5,2")), Is.True, "Should have 2 proteins with 5 modifications");
            Assert.That(lines.Any(l => l.StartsWith("7,1")), Is.True, "Should have 1 protein with 7 modifications");
            Assert.That(lines.Any(l => l.StartsWith("Total,")), Is.True);
        }

        [Test]
        public void EntrapmentGroupHistogramService_WritesDigestionHistogramCsv_WithCorrectValues()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var modCalc = new ModificationHistogramCalculator();
            var digCalc = new DigestionHistogramCalculator();
            var service = new EntrapmentGroupHistogramService(modCalc, digCalc);

            string dbName = "testdb";
            var digestionParams = AppHost.GetService<IDigestionParamsProvider>().GetParams(true, false);

            service.WriteDigestionHistogram(_groups, tempDir, digestionParams, dbName);

            var csvPath = Path.Combine(tempDir, $"{dbName}_DigestionHistogram.csv");
            Assert.That(File.Exists(csvPath), Is.True);

            var lines = File.ReadAllLines(csvPath);
            Assert.That(lines[0], Does.Contain("Peptides,Targets"));

            // Check for correct histogram values
            Assert.That(lines.Any(l => l.StartsWith("67,2")), Is.True, "Should have 2 proteins with 67 peptides");
            Assert.That(lines.Any(l => l.StartsWith("192,1")), Is.True, "Should have 1 protein with 192 peptides");
            Assert.That(lines.Any(l => l.StartsWith("Total,")), Is.True);
        }
    }
}