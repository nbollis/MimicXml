# AGENTS: MimicXml
This guide is for coding agents operating in `E:\Users\Nic\source\repos\mimicXml`.
It covers project layout, commands, style, and workflow conventions.
Treat it as the operating manual for autonomous edits in this repo.

## Project Summary
- Solution: `MimicXml.sln` with `Core` (services/library), `MimicXml` (CLI), and `Test` (NUnit).
- Purpose: generate entrapment XML databases for proteomics workflows, preserving biologically relevant annotations.
- Target framework: `.NET 8` (`net8.0`) with nullable and implicit usings enabled.
- Main external deps: `mzLib`, `CommandLineParser`, `Microsoft.Extensions.DependencyInjection`.
- Bundled native dependency: `MimicXml/mimic.exe`, copied to build output.
- CI: `.github/workflows/dotnet-desktop.yml` on `windows-latest`.

## High-Level Structure
- `MimicXml/Program.cs`: CLI entrypoint and command-line flow.
- `MimicXml/CommandLineSettings.cs`: CLI options and validation.
- `MimicXml/AppHost.cs`: dependency injection setup and service registration.
- `Core/Services/`: IO, mimic process runner, entrapment generation, histogram logic.
- `Core/Util/`: shared logging and helper utilities.
- `Test/`: NUnit tests, fixtures under `Test/**/TestData`, copied to output.

## Build Commands
- Restore all projects: `dotnet restore MimicXml.sln`
- Build Debug solution: `dotnet build MimicXml.sln --configuration Debug`
- Build Release solution: `dotnet build MimicXml.sln --configuration Release --no-restore`
- Build CLI only: `dotnet build MimicXml/MimicXml.csproj --configuration Release --no-restore`
- Build tests only: `dotnet build Test/Test.csproj --no-restore`
- Strict local check: `dotnet build MimicXml.sln -c Debug /warnaserror`
- Publish CLI (if needed): `dotnet publish MimicXml/MimicXml.csproj -c Release -r win-x64 --self-contained false`

## Test Commands
- Run all tests: `dotnet test Test/Test.csproj --configuration Debug`
- CI-like run: `dotnet test Test/Test.csproj --configuration Release --no-build --verbosity normal`
- Coverage: `dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" /p:CoverletOutputFormat=cobertura`

## Single-Test Commands (Important)
- Exact fully qualified test:
  - `dotnet test Test/Test.csproj --filter "FullyQualifiedName=Test.Entrapment.EntrapmentXml.ValidateInputPaths_ThrowsOnInvalidExtensions"`
- By class/namespace prefix:
  - `dotnet test Test/Test.csproj --filter "FullyQualifiedName~Test.Entrapment"`
- By test name substring:
  - `dotnet test Test/Test.csproj --filter "Name~TestEntrapmentXmlGeneration"`

## Linting and Formatting
- No standalone lint script; rely on SDK analyzers + NUnit analyzers via build/test.
- Verify formatting: `dotnet format --verify-no-changes`
- Apply formatting: `dotnet format`
- Keep warnings at zero; do not introduce analyzer noise.

## Running the CLI
- Typical run: `dotnet run --project MimicXml/MimicXml.csproj -- -x input.xml -e entrapment.fasta`
- Show help: `dotnet run --project MimicXml/MimicXml.csproj -- --help`
- If `-e` is omitted, CLI generates entrapment FASTA via `mimic.exe`.
- Histograms: `-m/--modHist` and `-d/--digHist` write CSVs near output XML.
- Prefer checking exit code (0 success, non-zero failure) in scripts.

## Code Style: Imports and Namespaces
- Use file-scoped namespaces (`namespace MimicXml;`).
- Keep `using` directives at top of file.
- Sort and deduplicate imports; remove unused usings.
- Group framework/third-party/internal usings clearly.
- Avoid fully-qualified type names inside method bodies when a using is clearer.

## Code Style: Types and Naming
- Respect nullable annotations; use `?` for optional refs and guard before use.
- Prefer `var` when RHS is obvious; otherwise use explicit type.
- Use PascalCase for types/methods/properties, camelCase for locals/parameters.
- Keep public API names descriptive; avoid unexplained abbreviations.
- Use object initializers (`new() { ... }`) for option/config assembly.

## Code Style: Control Flow and LINQ
- Prefer guard clauses and early returns for invalid input paths/states.
- Use LINQ for clear data transforms; switch to loops when logging/mutation is needed.
- Keep output deterministic (`OrderBy(...)`) before file emission.
- Avoid unnecessary async-over-sync patterns; async calls should be explicit and awaited where possible.

## Error Handling and Logging
- Validate input file existence/extensions before expensive work.
- Use precise exception types:
  - `ArgumentException` for invalid values
  - `ArgumentNullException` for null required arguments
  - `FileNotFoundException` for missing files
  - `InvalidOperationException` for unsupported runtime states
- Do not swallow exceptions silently; propagate to CLI exit behavior.
- Use `Core.Util.Logger.WriteLine` for user-facing progress; respect verbosity settings.

## DI and Service Patterns
- Register services in `AppHost.CreateBaseServices`.
- Resolve dependencies with `AppHost.GetService<T>()` where app pattern expects DI.
- Keep lifetimes consistent with existing registrations (mostly singletons).
- For strategy selection, follow existing factory pattern:
  - `Func<ModificationAssignmentStrategy, IModificationAssignmentService>`
- External process launches must set `UseShellExecute=false`.

## Test Conventions
- NUnit style: `Assert.That(actual, Is.EqualTo(expected))`.
- Use `[TestCase]` for parameterized scenarios.
- Use `TestContext.CurrentContext.TestDirectory` for fixture paths.
- Clean up generated files in tests (for example `*_Entrapment.xml`).
- Assembly-level setup exists in `Test/SetUpTests.cs`; avoid duplicating service bootstrapping patterns unnecessarily.

## Data and Assets
- Do not remove/rename bundled data assets without updating project metadata:
  - `MimicXml/mimic.exe`
  - `MimicXml/ProteolyticDigestion/proteases.tsv`
  - `MimicXml/Digestion/rnases.tsv`
- Ensure required fixtures remain under `Test/**/TestData` and are copied to output in `Test/Test.csproj`.

## CI Expectations
- Workflow restores, builds Release, ensures `coverlet.collector 6.0.2`, then runs tests with coverage.
- Keep changes compatible with Windows runners.
- Preserve coverage compatibility when touching test project dependencies.

## Cursor and Copilot Rules
- No Cursor rules were found in `.cursor/rules/`.
- No `.cursorrules` file was found.
- No Copilot rule file was found at `.github/copilot-instructions.md`.
- If any of these are added later, update this AGENTS file immediately.

## Recommended Agent Workflow
- Start by reading this file, then inspect relevant code paths only.
- Make minimal, targeted edits aligned with existing service boundaries.
- After edits, run at least targeted tests; run full tests when scope is broad.
- Run formatting check before handoff.
- In handoff notes, include commands executed and outcomes.

## Completion Checklist
- Build passes for touched projects.
- Relevant tests pass (single-test and/or suite as appropriate).
- Formatting and warnings are clean.
- No accidental generated artifacts remain checked in.
- README/docs updated if CLI options or behavior changed.

## Windows Command Notes
- Use PowerShell-friendly paths and quoting when paths contain spaces.
- Common directory/file commands: `ls`, `dir`, `Get-ChildItem`, `Get-Content`.
- Common search commands: `Select-String` (or `rg` if installed).
- Git basics: `git status`, `git diff`, `git log --oneline -n 20`.
- Prefer project-root execution so relative paths in docs/commands match.

## Troubleshooting Tips
- If `mimic.exe` is missing at runtime, verify `MimicXml/MimicXml.csproj` copy metadata is unchanged.
- If service resolution fails, ensure `AppHost.Services` is initialized before calling `AppHost.GetService<T>()`.
- If tests fail due to stale output, remove generated `*_Entrapment.xml` files and rerun.
- If coverage behavior changes, check `coverlet.collector` version alignment with CI (`6.0.2`).
- If command-line parsing looks wrong, run help output and confirm option attributes in `CommandLineSettings.cs`.

## Agent Scope Guidance
- Prefer minimal changes over broad refactors unless the task explicitly requires refactoring.
- Keep edits localized to the relevant layer (`Core` service vs CLI orchestration vs tests).
- Do not remove existing test fixtures or bundled proteomics assets without replacing equivalent coverage.
- When adding behavior, add or update tests in `Test/` in the same change.
- Preserve public CLI behavior unless the task explicitly requests a breaking change.
