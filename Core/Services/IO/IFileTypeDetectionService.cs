using System.Text;
using System.Xml;
using Omics;

namespace Core.Services.IO;
public interface IFileTypeDetectionService
{
    BioPolymerDbFileType DetectFileType(string filePath);
    BioPolymerDbFileType DetectFileType(List<IBioPolymer> bioPolymers, string outputPath);
}
public enum BioPolymerDbFileType
{
    ProteinFasta,
    ProteinXml,
    RnaFasta,
    RnaXml,
    Unknown
}

public readonly struct SequenceCharCounts
{
    public int A { get; }
    public int C { get; }
    public int G { get; }
    public int T { get; }
    public int U { get; }
    public int Total { get; }

    public SequenceCharCounts(int a, int c, int g, int t, int u, int total)
    {
        A = a;
        C = c;
        G = g;
        T = t;
        U = u;
        Total = total;
    }
}

public class FileTypeDetectionService : IFileTypeDetectionService
{
    private static readonly double NucleotideThresholdForRnaDetection = 0.9;
    public BioPolymerDbFileType DetectFileType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".fasta" || extension == ".fa" || extension == ".fna")
        {
            string sequence = GetFirstSequencesFromFasta(filePath, 10);
            var counts = CountSequenceChars(sequence);

            if (counts.Total == 0)
                return BioPolymerDbFileType.Unknown;

            int acgtu = counts.A + counts.C + counts.G + counts.T + counts.U;
            double acgtuRatio = (double)acgtu / counts.Total;

            return acgtuRatio > NucleotideThresholdForRnaDetection
                ? BioPolymerDbFileType.RnaFasta
                : BioPolymerDbFileType.ProteinFasta;
        }

        if (extension == ".xml")
        {
            string sequence = GetFirstSequencesFromXml(filePath, 10);
            var counts = CountSequenceChars(sequence);

            if (counts.Total == 0)
                return BioPolymerDbFileType.Unknown;

            int acgtu = counts.A + counts.C + counts.G + counts.T + counts.U;
            double acgtuRatio = (double)acgtu / counts.Total;

            return acgtuRatio > NucleotideThresholdForRnaDetection
                ? BioPolymerDbFileType.RnaXml
                : BioPolymerDbFileType.ProteinXml;
        }

        return BioPolymerDbFileType.Unknown;
    }

    public BioPolymerDbFileType DetectFileType(List<IBioPolymer> bioPolymers, string outputPath)
    {
        if (bioPolymers == null || !bioPolymers.Any())
            return BioPolymerDbFileType.Unknown;

        string extension = Path.GetExtension(outputPath).ToLowerInvariant();

        // Analyze sequence content
        var sequences = GetFirstSequencesFromList(bioPolymers);
        var counts = CountSequenceChars(sequences);

        int acgtu = counts.A + counts.C + counts.G + counts.T + counts.U;
        double ratio = (double)acgtu / counts.Total;

        bool isRna =  ratio > NucleotideThresholdForRnaDetection;

        if (extension == ".fasta" || extension == ".fa" || extension == ".fna")
            return isRna ? BioPolymerDbFileType.RnaFasta : BioPolymerDbFileType.ProteinFasta;

        if (extension == ".xml")
            return isRna ? BioPolymerDbFileType.RnaXml : BioPolymerDbFileType.ProteinXml;

        return BioPolymerDbFileType.Unknown;
    }

    public static SequenceCharCounts CountSequenceChars(string sequence)
    {
        int a = 0, c = 0, g = 0, t = 0, u = 0, total = 0;
        foreach (char ch in sequence)
        {
            switch (ch)
            {
                case 'A': a++; break;
                case 'C': c++; break;
                case 'G': g++; break;
                case 'T': t++; break;
                case 'U': u++; break;
            }
            if (!char.IsWhiteSpace(ch))
                total++;
        }
        return new SequenceCharCounts(a, c, g, t, u, total);
    }

    private static string GetFirstSequencesFromFasta(string filePath, int maxLines)
    {
        var sequence = new StringBuilder();
        int linesRead = 0;
        using (var reader = new StreamReader(filePath))
        {
            string? line;
            while ((line = reader.ReadLine()) != null && linesRead < maxLines)
            {
                if (line.StartsWith(">"))
                    continue;
                sequence.Append(line.Trim().ToUpperInvariant());
                linesRead++;
            }
        }
        return sequence.ToString();
    }

    private static string GetFirstSequencesFromXml(string filePath, int maxEntries)
    {
        var sequence = new StringBuilder();
        int entriesRead = 0;
        using (var reader = XmlReader.Create(filePath))
        {
            while (reader.Read() && entriesRead < maxEntries)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "sequence")
                {
                    var seq = reader.ReadElementContentAsString().Trim().ToUpperInvariant();
                    sequence.Append(seq);
                    entriesRead++;
                }
            }
        }
        return sequence.ToString();
    }

    public static string GetFirstSequencesFromList(List<IBioPolymer> bioPolymers, int maxLength = 500)
    {
        var sequence = new StringBuilder();
        foreach (var bioPolymer in bioPolymers)
        {
            sequence.Append(bioPolymer.BaseSequence.ToUpperInvariant());
            if (sequence.Length >= maxLength)
                break;
        }
        return sequence.ToString();
    }
}