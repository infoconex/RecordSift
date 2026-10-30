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

All commands must succeed; the resulting `TestResults/nunit-results.xml` must contain at least three tests and zero failures. Tests are also runnable in the included issue-57 Windows PR smoke workflow.

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

The PR for issue #57 must link an observed Windows workflow run and report actual restore/build/test results before merge. The issue-57 PR smoke workflow checks that tests are **discovered** (at least three) and have no failures, rather than silently treating an empty test assembly as success. Issue #64 separately owns the maintained release-branch Windows CI gate, publishing test artifacts and dependency/security hardening.
