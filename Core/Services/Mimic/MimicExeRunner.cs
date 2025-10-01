using System.Diagnostics;

namespace Core.Services.Mimic;

public interface IMimicExeRunner
{
    Task<(int OutputCode, string EntrapmentPath)> RunAsync(MimicParams arguments, CancellationToken cancellationToken = default);
}

public class MimicExeRunner : IMimicExeRunner
{
    private readonly string _exePath;

    public MimicExeRunner()
    {
        // Assumes mimic.exe is copied to the output directory (e.g., via project file settings)
        var solutionDir = AppContext.BaseDirectory;
        _exePath = Path.Combine(solutionDir, "mimic.exe");
        _exePath = Path.GetFullPath(_exePath);

        if (!File.Exists(_exePath))
            throw new FileNotFoundException($"Could not find mimic.exe at {_exePath}");
    }

    public async Task<(int OutputCode, string EntrapmentPath)> RunAsync(MimicParams arguments, CancellationToken cancellationToken = default)
    {
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));
        if (string.IsNullOrWhiteSpace(arguments.InputFastaPath))
            throw new ArgumentException("InputFastaPath is required in MimicParams");
        if (string.IsNullOrWhiteSpace(arguments.OutputFastaPath))
            throw new ArgumentException("OutputFastaPath is required in MimicParams");
        if (!File.Exists(arguments.InputFastaPath))
            throw new FileNotFoundException($"Input FASTA file does not exist: {arguments.InputFastaPath}");


        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _exePath,
            Arguments = arguments.ToArgString(),
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, arguments.OutputFastaPath!);
    }
}