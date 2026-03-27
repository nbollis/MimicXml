# Task Completion Checklist
- Run the appropriate `dotnet build` (Debug or Release) for touched projects; keep analyzer warnings at zero and confirm `mimic.exe`/asset metadata stay intact.
- Execute relevant `dotnet test` commands (targeted test or full suite) after code changes; use the specific `--filter` patterns if only a subset is required.
- Verify formatting with `dotnet format --verify-no-changes`; if formatting is needed, run `dotnet format` before committing.
- Confirm CLI behavior (via `dotnet run --project MimicXml/MimicXml.csproj -- ...` or the example commands) when touching command-line flow or argument parsing.
- Ensure generated fixtures like `*_Entrapment.xml` are cleaned up in tests and no temporary artifacts leak into commits.
- Update README/docs whenever CLI options or behavior change, and mention command outputs/outcomes in handoff notes.