# Constructed legacy .NET Framework 4.8 baseline (issue #57)

This is a **deliberately constructed, historical-style** project configuration for an incremental modernization demonstration. It is not recovered production source.

## Prerequisites

- Windows with Visual Studio 2019/2022 or compatible Build Tools with the **.NET Framework 4.8 targeting/developer pack** and MSBuild.
- NuGet CLI (`nuget.exe`) available on PATH; internet access for the initial package restore.
- No .NET SDK-style project tooling, Excel installation, or external service is required.

## Restore, build and execute the baseline tests

From the repository root in PowerShell (using a Developer PowerShell command prompt where `msbuild` is on PATH):

```powershell
nuget restore .\RecordSift.sln -NonInteractive -PackagesDirectory packages
msbuild .\RecordSift.sln /m /t:Rebuild /p:Configuration=Release '/p:Platform=Any CPU'
New-Item -ItemType Directory -Force -Path .\TestResults | Out-Null
.\packages\NUnit.ConsoleRunner.3.17.0\tools\nunit3-console.exe .\tests\RecordSift.Tests\bin\Release\RecordSift.Tests.dll '--result=TestResults/nunit-results.xml;format=nunit3'
```

All commands must succeed; the resulting `TestResults/nunit-results.xml` must contain at least three tests and zero failures. Windows release-branch CI from issue #64 runs the same commands on PRs, release pushes and manual dispatch, and uploads NUnit XML evidence.

## Current architecture (after engineering issues #95, #97 and #99)

The original issue #57 `src/RecordSift` entry below is **historical**, not a current build path. The live historical-format solution now contains `src/RecordSift.Domain`, `src/RecordSift.Application` and `src/RecordSift.Infrastructure.Csv` as separate net48 assemblies. The tests are also separate: vendor-independent `tests/RecordSift.Tests` and real-adapter `tests/RecordSift.Infrastructure.Csv.Tests`. Follow the current Windows workflow for the complete two-suite NUnit and dependency-audit commands. The legacy flat `RecordSift` public namespace is superseded by [ADR 0015](decisions/0015-layer-aligned-public-namespaces.md).

## Project structure and versioned dependencies

| Path | Purpose |
| --- | --- |
| `RecordSift.sln` | Traditional Visual Studio solution format, Debug/Release and Any CPU |
| `src/RecordSift/RecordSift.csproj` | Non-SDK-style .NET Framework 4.8 reusable class library |
| `tests/RecordSift.Tests/RecordSift.Tests.csproj` | Non-SDK-style .NET Framework 4.8 NUnit assembly with a production project reference |
| `tests/RecordSift.Tests/packages.config` | Exact NUnit 3.14.0 and NUnit.ConsoleRunner 3.17.0 versions |
| `tests/RecordSift.Tests/BaselineAssemblyTests.cs` | NUnit tests for assembly load/copy, target framework and traditional assembly metadata |

NUnit 3.14.0 supports .NET Framework 4.8 (via its net45 assembly) and uses the MIT license. NUnit.ConsoleRunner 3.17.0 runs NUnit 3 assemblies and is MIT licensed. The runner is a test execution tool only; the production library intentionally has **no NuGet dependencies** in this issue. `packages.config` pins version numbers but is not a cryptographic lockfile. Review dependency vulnerability status and test runner upgrade options in issue #64 before release.

The NUnit tests verify only that the historical executable baseline is properly assembled. They do **not** verify any FR/NFR acceptance scenarios. Accordingly, the 109 scenario rows in `docs/testing/traceability.md` remain **Planned**. CSV/XLSX parsing, public import APIs, JSON contract fixtures, and functional tests belong to separately tracked requirements.

## Evidence and limitations

The PR for issue #57 must link an observed Windows workflow run and report actual restore/build/test results before merge. The issue-57 PR smoke workflow checks that tests are **discovered** (at least three) and have no failures, rather than silently treating an empty test assembly as success. Issue #64 implements maintained release-branch Windows CI, publishing NUnit XML and NuGet audit logs as artifacts. The independent dependency-audit job checks exact pinned packages and fails on NuGet audit warnings NU1900–NU1905; a passing audit means no warnings were reported on that run, not that future vulnerabilities are impossible.

## Observed initial baseline evidence

Issue #57 was squash-merged after a successful Windows build and **3 of 3** NUnit baseline tests in [Actions run #41017804680](https://github.com/infoconex/RecordSift/actions/runs/41017804680). The requirement scenarios are still Planned; assembly smoke tests cannot certify CSV/XLSX behavior.

## Issue #85 CSV adapter addition

The initial issue #57 dependency inventory above is historical evidence; it is not the current total after CSV adapter integration. Issue #85 adds the independent traditional `src/RecordSift.CsvHelperAdapter` project and its pinned CSV runtime dependency graph; core `RecordSift` remains free of NuGet parser packages. See [ADR 0008](decisions/0008-csvhelper-isolated-adapter.md) for the current design and [PR #87](https://github.com/infoconex/RecordSift/pull/87) for observed verification.
