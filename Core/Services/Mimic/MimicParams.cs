using System.Text;

namespace Core.Services.Mimic;
public class MimicParams
{
    public string? InputFastaPath { get; set; }
    public string? OutputFastaPath { get; set; }
    public int MultFactor { get; set; } = 9;
    public bool NoDigest { get; set; } = true;
    public int TerminalResiduesToRetain { get; set; } = 0;

    /// <summary>
    /// Mimic always uses -A, -e, and --replaceI flags
    /// </summary>
    /// <returns></returns>
    public string ToArgString()
    {
        if (string.IsNullOrWhiteSpace(InputFastaPath))
            throw new ArgumentException("InputFastaPath is required for MimicParams");
        if (string.IsNullOrWhiteSpace(OutputFastaPath))
            throw new ArgumentException("OutputFastaPath is required for MimicParams");

        if (!NoDigest && TerminalResiduesToRetain > 0)
            throw new ArgumentException("TerminalResiduesToRetain can only be set when NoDigest is true");

        var sb = new StringBuilder();
        sb.Append($"\"{InputFastaPath}\"");
        sb.Append($" -o \"{OutputFastaPath}\"");
        sb.Append(" -A -e --replaceI"); // Always use these flags
        sb.Append($" -m {MultFactor}");
        if (NoDigest)
            sb.Append(" -N");
        if (TerminalResiduesToRetain > 0)
            sb.Append($" -T {TerminalResiduesToRetain}");

        return sb.ToString();
    }
}
