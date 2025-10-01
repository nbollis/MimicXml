# mimicXml Command-Line Tool

## Overview

`mimicXml` is a .NET 8 command-line utility for generating entrapment XML files for proteomics workflows. It can use a user-supplied entrapment FASTA or generate one on-the-fly using a bundled version of the [mimic](https://github.com/percolator/mimic) executable (forked from commit [`504df5b`](https://github.com/percolator/mimic/commit/504df5b), see pull request [#4](https://github.com/percolator/mimic/pull/4)).

## Command-Line Options


| Option (short/long)      | Required | Default | Description |
|--------------------------|----------|---------|-------------|
| `-x`, `--targetXml`      | Yes      |         | Starting XML file path (`.xml` or `.xml.gz`) |
| `-e`, `--entrapmentFasta`| No       | null    | Entrapment FASTA file path (`.fasta` or `.fa`, can be gzipped). If not set, mimic will generate one. |
| `-o`, `--output`         | No       | null    | Output XML file path. If not set, output will be in the same location as the original xml. |
| `-v`, `--verbose`        | No       | true    | Verbose output to console. |
| `-m`, `--modHist`        | No       | false   | Generate a histogram of modification frequencies in the entrapment proteins. |
| `-d`, `--digHist`        | No       | false   | Generate a histogram of digestion products in the entrapment proteins. |
| `-t`, `--isTopDown`      | No       | true    | Generate entrapment proteins for top-down searches. If false, generates for bottom-up. |
| `--mimicMultFactor`      | No       | 9       | Number of times the database should be multiplied (higher = more entrapment sequences, longer runtime). |
| `--mimicRetainTerm`      | No       | 0       | Number of terminal residues to retain if running in top-down mode (default: 0 for bottom-up, 4 for top-down). |


### mimic Parameters Used

The parameters passed to `mimic.exe` are constructed in [`MimicParams.cs`](Core/Services/Mimic/MimicParams.cs) and always include:

- `-A` : Retain the original accession in the mimic output.
- `-e` : Infer amino acid frequency empirically from the input FASTA.
- `--replaceI` : Treat I and L as equivalent when checking for duplicate sequences.
- `-p` : Prefix to mimic proteins (Default: "mimic|Random_").
- `-s` : Ratio of shared peptides that will stay preserved in the mimic database (Default: 0.0).
- `-S` : Set seed of the random number generator (Default: 1).

Other parameters are settable via the command line or hardcoded:

| mimic Flag         | .NET Property                | Description |
|--------------------|-----------------------------|-------------|
| `-m`               | `MultFactor`                | Number of times the database is multiplied (default: 9). |
| `-N`               | `NoDigest`                  | Do not digest the sequence before scrambling (set for top-down). |
| `-T`               | `TerminalResiduesToRetain`  | Number of N- and C-terminal residues to retain during scrambling (NoDigest only, default: 4). |
| `-A`               | (always set)                | Retain original accession. |
| `-e`               | (always set)                | Empirically infer AA frequency. |
| `--replaceI`       | (always set)                | Treat I and L as equivalent for duplicate checking. |
| `-p`               | (always set)                | Prefix to mimic proteins (Default: "mimic|Random_"). |
| `-s`               | (always set)                | Ratio of shared peptides that will stay preserved in the mimic database (Default: 0.0). |
| `-S`               | (always set)                | Set seed of the random number generator (Default: 1). |

## Example Usage

`mimicXml.exe -x input.xml --mimicMultFactor 5 --mimicRetainTerm 2`
This will generate an entrapment FASTA using mimic with a multiplication factor of 5 and retain 2 terminal residues, then produce the output XML.

`mimicXml.exe -x input.xml -e entrapment.fasta -o output.xml`
This will use the provided entrapment FASTA and generate the output XML at the specified location.

## System Requirements
* Environment:
  * 64-bit operating system
  * .NET Core 8.0:
     * Windows: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.401-windows-x64-installer
     * macOS, x64 Intel processor: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.401-macos-x64-installer
     * macOS, ARM Apple Silicon processor: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.401-macos-arm64-installer
     * Linux: https://learn.microsoft.com/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website
