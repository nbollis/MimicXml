Restore/build/test:
- dotnet restore MimicXml.sln
- dotnet build MimicXml.sln --configuration Debug
- dotnet build MimicXml.sln --configuration Release --no-restore
- dotnet test Test/Test.csproj --configuration Debug
- dotnet test Test/Test.csproj --configuration Release --no-build --verbosity normal

Single-test commands:
- dotnet test Test/Test.csproj --filter "FullyQualifiedName=Test.Entrapment.EntrapmentXml.ValidateInputPaths_ThrowsOnInvalidExtensions"
- dotnet test Test/Test.csproj --filter "FullyQualifiedName~Test.Entrapment"
- dotnet test Test/Test.csproj --filter "Name~TestEntrapmentXmlGeneration"

Formatting/lint-like checks:
- dotnet format --verify-no-changes
- dotnet format
- dotnet build MimicXml.sln -c Debug /warnaserror

Coverage:
- dotnet test Test/Test.csproj --collect:"XPlat Code Coverage" /p:CoverletOutputFormat=cobertura

Run CLI:
- dotnet run --project MimicXml/MimicXml.csproj -- -x input.xml -e entrapment.fasta
- dotnet run --project MimicXml/MimicXml.csproj -- --help

Windows utility commands commonly used:
- git status, git diff, git log
- ls (PowerShell alias), dir, Get-ChildItem
- Select-String for text search (or rg if installed)