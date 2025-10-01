namespace Core.Services.Mimic;
public class MimicParams
{
    public string? InputFastaPath { get; set; }
    public string? OutputFastaPath { get; set; }
    public int MultFactor { get; set; } = 1;
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

        return $"\"{InputFastaPath}\" -o \"{OutputFastaPath}\" -m {MultFactor} {(NoDigest ? "-N" : "")} -T {TerminalResiduesToRetain} -A -e --replaceI";
    }
}
