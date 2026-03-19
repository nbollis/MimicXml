# AGENTS: MimicXml
This guide orients autonomous agents working in `E:\Users\Nic\source\repos\mimicXml`.
It summarizes how to build, test, lint, and follow the house coding style.
Treat it as the single source of truth while automating work in this repo.

## Repository Snapshot
- Solution: `MimicXml.sln` with `Core` (library), `MimicXml` (CLI), and `Test` (NUnit).
- Entry CLI sources live in `MimicXml/` and depend on `Core/` service abstractions.
- Shared utilities (histograms, IO, mimic runner, logger) live in `Core/Services` and `Core/Util`.
- Test assets such as FASTA/XML fixtures live under `Test/**/TestData` and are copied to output.
- Binary dependency `MimicXml/mimic.exe` is shipped with the repo and copied at build.
- No Cursor rule files and no `.github/copilot-instructions.md` are present in this workspace.
- CI is configured via `.github/workflows/dotnet-desktop.yml` targeting Windows and .NET 8.

## Toolchain & Environment
- Target framework is `net8.0`; install .NET 8 SDK before running commands.
- `CommandLineParser`, `Microsoft.Extensions.DependencyInjection`, and `mzLib` are the primary external packages for the CLI.
- Tests use `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`, and `coverlet.collector` (6.x per CI step).
- `mimic.exe` is invoked by `Core.Services.Mimic.MimicExeRunner`; ensure it exists in the build output.
- Development happens on Windows, but commands run cross-platform except for invoking the Windows-only `mimic.exe`.
- Nullable reference types and implicit usings are enabled across projects; treat warnings as build failures.
- Service wiring relies on `Microsoft.Extensions.DependencyInjection`; respect the `AppHost` pattern.

## Build Commands
- Restore all projects: `dotnet restore MimicXml.sln`.
- Build everything (Debug): `dotnet build MimicXml.sln --configuration Debug`.
- Build optimized CLI: `dotnet build MimicXml/MimicXml.csproj --configuration Release --no-restore`.
- Build tests only: `dotnet build Test/Test.csproj --no-restore`.
- Fast inner-loop: `dotnet build MimicXml.sln -c Debug /warnaserror` to keep warnings from drifting.
- Always run `dotnet restore` before the first build in a fresh workspace or CI runner.
- The CLI currently has no separate packaging script; produce binaries via `dotnet publish -c Release -r win-x64 --self-contained false` if needed.

## Test Commands
- Full suite: `dotnet test Test/Test.csproj --configuration Debug`.
- Release-mode tests (matches CI): `dotnet test Test/Test.csproj --configuration Release --no-build --verbosity normal`.
- Collect coverage locally: `dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" /p:CoverletOutputFormat=cobertura`.
- Run a single test by fully qualified name:
  - Example: `dotnet test Test/Test.csproj --filter "FullyQualifiedName=Test.Entrapment.EntrapmentXml.ValidateInputPaths_ThrowsOnInvalidExtensions"`.
- Filter by class prefix: `dotnet test Test/Test.csproj --filter "FullyQualifiedName~Test.Entrapment"`.
- When debugging, set `TestContext.CurrentContext.TestDirectory` aware paths for fixture files.
- Tests rely on services from `AppHost`; ensure `AppHost.Services` is initialized inside `[OneTimeSetUp]`.

## Linting & Formatting
- No custom analyzers beyond the default SDK analyzers and `NUnit.Analyzers`; keep the code clean to avoid warnings.
- Use `dotnet format --verify-no-changes` in CI-style validations, or `dotnet format` to apply the repo-wide conventions.
- Maintain file-scoped namespaces (`namespace Foo;`) and implicit usings; avoid redundant namespace blocks.
- ReSharper dictionary additions live in `mimicXml.sln.DotSettings`; keep proteomics terms there instead of disabling spell-checkers.
- Prefer `var` when the type is obvious from the right-hand side; spell out the type when clarity is needed.
- Keep using directives sorted and deduplicated; remove unused imports to prevent warnings.

## Running the CLI
- Default invocation: `dotnet run --project MimicXml/MimicXml.csproj -- -x input.xml -e entrapment.fasta`.
- Generate an entrapment FASTA on the fly by omitting `-e`; ensure `mimic.exe` can run on your platform.
- Use `--mimicMultFactor` and `--mimicRetainTerm` to control mimic behavior; CLI validates extensions before running.
- `-m/--modHist` and `-d/--digHist` emit histograms via `Core.Services.Entrapment.IEntrapmentGroupHistogramService` into the output folder.
- Verbose logging uses `Core.Util.Logger` timestamps; pass `-v false` to quiet console output.
- Temporary FASTA/XML intermediates are written to `%TEMP%`; they are cleaned through `ITempFileCleanupService` on exit.
- When scripting, rely on process exit codes: 0 = success, non-zero = argument/IO/process failure.

## Imports & Namespaces
- Stick to file-scoped namespaces with PascalCase matching the folder path (`namespace MimicXml;`).
- Group framework/BCL/third-party usings first, then internal project namespaces; keep blank lines between logical groups only if it aids clarity.
- Avoid fully qualified names inside the body; if you need a type repeatedly, add a `using` directive.
- Place `using` directives at the top of each file; do not rely on implicit global usings for clarity-critical dependencies.
- When adding new projects, update `.sln` references instead of using relative `ProjectReference` hacks.

## Types, Nullability, and Objects
- Nullable reference types are enforced; mark optional values as `string?` and validate before use.
- Use `ArgumentNullException.ThrowIfNull` or guard clauses early in methods (see `MimicExeRunner`).
- Initialize options objects with object initializers (`new() { ... }`) to keep defaults readable.
- Prefer `record` or `record struct` when representing immutable data; existing models use classes, so keep consistency unless there is a strong reason to diverge.
- For DI-constructed services, use primary constructors (`public class Foo(IDep dep) : BaseService`), mirroring `EntrapmentXmlGenerator`.
- Keep `Verbosity` plumbing by inheriting from `BaseService` whenever the service logs user-facing messages.

## Collections & LINQ
- Favor LINQ for declarative transformations but break into loops when intermediate logging or mutation is required.
- Respect the existing pattern of `var groups = loadingService.LoadAndParseProteins([...]); groups.EnsureUniqueAccessions();` before iterating.
- Order results deterministically before writing files (e.g., `OrderBy(p => p.Position)` when copying modifications).
- When collecting errors, use `List<string> errors = new();` and pass by `ref` into helper methods as done in modification assignment.
- Avoid asynchronous LINQ inside synchronous workflows; perform async operations via `Task` APIs explicitly (`await mimic.RunAsync(...)`).

## Error Handling & Logging
- Validate file extensions and existence before heavy work (`EntrapmentXmlGenerator.ValidateInputPaths`, `CommandLineSettings.ValidateCommandLineSettings`).
- Throw `ArgumentException` for invalid parameter values, `FileNotFoundException` for missing files, and `InvalidOperationException` for unsupported types.
- Use `Logger.WriteLine` for high-level progress; include `layer` indentation to convey nested operations.
- Propagate mimic exit codes up to `Program.Main` and return them so scripts can react.
- Favor early returns and guard clauses to keep methods linear and easy to prove correct.
- Do not swallow exceptions; let them bubble so CLI exit codes reflect actual failure conditions.

## Services, DI, and Process Launchers
- Register services through `AppHost.CreateBaseServices`; each additional service should have a singleton registration unless stateful.
- Retrieve services via `AppHost.GetService<T>()` rather than `new` to honor lifetime expectations.
- When adding a new service that depends on runtime options, inject a factory (as seen with the modification assignment strategy delegate).
- Keep `Func<ModificationAssignmentStrategy, IModificationAssignmentService>` factory updated when new strategies are added.
- `MimicExeRunner` resolves `mimic.exe` relative to `AppContext.BaseDirectory`; copy new native assets via `<None Update=... CopyToOutputDirectory>` metadata.
- Any external process must be invoked with `UseShellExecute=false` and `CreateNoWindow=false` unless there is a console UX need.

## File & Data Handling
- IO abstractions live under `Core.Services.IO`; reuse them instead of accessing files directly from CLI layers.
- Use `CompositeBioPolymerDbReader/Writer` to automatically route between FASTA/XML and Protein/RNA file types.
- Keep `BioPolymerDbReaderOptions` defaults centralized in `CompositeBioPolymerDbReader.DefaultDbReaderOptions` and allow overrides via optional parameters.
- Temp artifacts go through `ITempFileCleanupService` to avoid leaving large FASTA files on disk.
- Entrapment output filenames follow `*_Entrapment.xml`; use `EntrapmentXmlGenerator.GetOutputPath` to stay consistent.
- Preserve existing test data layout; new fixtures belong under the most specific subfolder in `Test/**/TestData` and must be marked `CopyToOutputDirectory` in `Test.csproj`.

## Testing Patterns
- NUnit `[SetUpFixture]` (`Test/SetUpTests.cs`) bootstraps DI once per test assembly—reuse it rather than building ad-hoc containers.
- Standard assertion style is `Assert.That(actual, Is.EqualTo(expected))` with helpful failure messages when loops are involved.
- Multi-case tests rely on `[TestCase]` attributes; prefer descriptive parameter names over cryptic integers unless referencing enumerated data.
- Keep deterministic file paths via `TestContext.CurrentContext.TestDirectory` and `Path.Combine` as shown in `Test.Entrapment.EntrapmentXml`.
- Clean any files you create in tests (e.g., delete generated XML at the end of `TestEntrapmentXmlGeneration`).
- Use `FullyQualifiedName` filters when running a subset of tests locally to reproduce CI failures quickly.

## CI Expectations
- Workflow `.github/workflows/dotnet-desktop.yml` restores, builds Release, re-adds `coverlet.collector 6.0.2`, then runs tests with coverage.
- Coverage reports are uploaded to Codecov; keep coverlet-compatible settings intact when touching `Test/Test.csproj`.
- Ensure `dotnet add Test/Test.csproj package coverlet.collector -v 6.0.2` remains idempotent—CI re-runs it each build.
- Keep build warnings at zero; CI treats warnings as failures once `/warnaserror` is toggled locally.
- Push changes that build on Windows runners; mimic invocation and binary copying assume a Windows filesystem.

## Cursor/Copilot Rules
- There are currently no Cursor rule files (`.cursor/rules/`, `.cursorrules`) in the repo.
- There is no `.github/copilot-instructions.md` file; follow this AGENTS guide for assistant behavior.
- If such rule files are added later, update this section immediately so downstream agents inherit the new policies.

## Everyday Workflow Reminders
- Open PRs should document command lines you executed (build, test, format) and any mimic-related prerequisites.
- When modifying CLI options, update both `CommandLineSettings` attributes and `README.md` tables to keep docs synchronized.
- Keep AGENTS.md roughly ~150 lines when editing; preserve this level of detail for future agents.
- Never delete or rename bundled data (`ProteolyticDigestion/*.tsv`, `Digestion/rnases.tsv`, `mimic.exe`) without adjusting `<CopyToOutputDirectory>` metadata.
- Prefer targeted commits that align with repo conventions; mention whether you ran `dotnet test` locally.
- If you need new third-party tools, add them via `PackageReference` and document the change here.

## Logging & Diagnostics Discipline
- `Core.Util.Logger` timestamps every line; keep human-readable messages free of stack traces unless debugging.
- Respect the `Verbose` property inherited from `BaseService`; never print to console when `Verbose` is false.
- For long-running steps (mimic invocation, histogram generation), log start and completion messages with indentation.
- Use `Debug.Assert` only inside `#if DEBUG` blocks; production builds should rely on guard clauses instead.
- When wrapping external processes, capture exit codes and bubble them up so `Program.Main` can surface them to scripts.

## Histograms & Analysis Outputs
- Modification histograms are generated via `IEntrapmentGroupHistogramService.WriteModificationHistogram`; pass the same filename roots as database outputs.
- Digestion histograms require `IDigestionParams`; re-use `IDigestionParamsProvider` to keep top-down vs. bottom-up settings synchronized.
- Histogram CSVs land alongside the XML output; keep names deterministic so tests can locate them easily.
- When adding metrics, update both the histogram service implementations and CLI switches; default values must keep existing behavior.
- Tests asserting histogram output should clean temporary files to avoid polluting `TestResults` directories.

## Debugging & Troubleshooting
- If `mimic.exe` cannot be found, confirm `MimicXml.csproj` still copies it via `<None Update="mimic.exe">` metadata.
- When `AppHost.Services` is null, ensure `CreateBaseServices` has been invoked before requesting dependencies.
- Most services assume unique protein accessions; call `EnsureUniqueAccessions()` after merging databases.
- For Windows path issues, rely on `Path.Combine` and avoid string concatenation; tests expect OS-appropriate separators.
- To diagnose command-line parsing, run `dotnet run -- --help` to view the `CommandLineParser` table produced in `Program.DisplayHelp`.
- If tests fail because of leftover XML output, delete `_Entrapment.xml` artifacts before rerunning; the tests assume a clean slate.

## Documentation & README
- Update `README.md` whenever user-facing options change, especially the command-line options table and example usage.
- Link to upstream mimic references when upgrading the bundled executable so provenance stays clear.
- Keep badges (Codecov) in sync with project reality; adjust URLs if the repository owner or branch names change.
- Mention new system requirements or platform caveats in README to reduce surprises for CLI users.
- Maintain consistent Markdown tables; align pipes so diffs stay readable.

## Git Workflow Expectations
- Work from feature branches; keep `master` clean so CI stays green.
- Run `git status` before staging to avoid accidentally committing build outputs or test fixtures.
- Do not rewrite history on shared branches; use `git merge` or `rebase` locally before pushing if needed.
- Reference issue numbers in commit messages when applicable; keep messages imperative and scoped.
- Never commit secrets or large binaries beyond the approved datasets already under version control.

## Final Checklist
- Before handing work back, verify builds/tests you touched and capture the commands in your notes.
- Confirm generated files (XML, FASTA, histogram CSVs) are cleaned up unless explicitly required.
- Ensure this AGENTS file stays current—update relevant sections whenever workflows or tooling evolve.
