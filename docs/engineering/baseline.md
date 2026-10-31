# Constructed legacy .NET Framework 4.8 baseline (issue #57)

This is a **deliberately constructed, historical-style** project configuration for an incremental modernization demonstration. It is not recovered production source.

## Prerequisites

- Windows with Visual Studio 2019/2022 or compatible Build Tools with the **.NET Framework 4.8 targeting/developer pack** and MSBuild.
- NuGet CLI (`nuget.exe`) available on PATH; internet access for the initial package restore.
- No .NET SDK-style project tooling, Excel installation, or external service is required.

## Restore, build and execute the current net48 solution

The **original issue #57 smoke tests are historical**, and the former `tests/RecordSift.Tests` project has been replaced. Do not copy the obsolete initial smoke-test path into current release instructions.

From the repository root on Windows (Visual Studio Developer PowerShell or equivalent):

```powershell
nuget restore .\RecordSift.sln -NonInteractive -PackagesDirectory packages -ConfigFile NuGet.Config
msbuild .\RecordSift.sln /m /t:Rebuild /p:Configuration=Release '/p:Platform=Any CPU'
.\scripts\run_tests.ps1 -Verification Acceptance
```

For full release acceptance, the [Windows build/test workflow](../../.github/workflows/windows-net48.yml) executes **five** NUnit assemblies (Domain, Application, CSV, Reporting and XLSX), public API/JSON compatibility, XML docs, Office-free and offline checks, pinned-dependency security audit and unpublished NuGet packaging. The [independent installed-package workflow](../../.github/workflows/verify-rc-consumer.yml) tests both `0.1.0-rc.1` and `0.1.0` local packages on Windows/net48. The accepted baseline is **379/379 NUnit** and **425 compiled public API entries**; see [readiness](release-0.1-readiness.md) for the exact passing SHA and CI evidence.

## Current architecture (after the release 0.1 engineering migrations)

The initial issue #57 `src/RecordSift` directory and `tests/RecordSift.Tests` project described below are **historical**, not current build paths. The active solution has five physical production assemblies: `RecordSift.Domain`, `RecordSift.Application`, `RecordSift.Infrastructure.Csv`, `RecordSift.Infrastructure.Xlsx` and `RecordSift.Infrastructure.Reporting`. Five corresponding NUnit assemblies isolate Domain, Application, CSV, XLSX and Reporting tests. See [architecture](architecture.md), [ADR 0013](decisions/0013-physical-domain-application-infrastructure-assemblies.md), and [current testing strategy](../testing/strategy.md).

## Original issue #57 project structure and dependency inventory (historical only)

| Path | Purpose |
| --- | --- |
| `RecordSift.sln` | Traditional Visual Studio solution format, Debug/Release and Any CPU |
| `src/RecordSift/RecordSift.csproj` | Non-SDK-style .NET Framework 4.8 reusable class library |
| `tests/RecordSift.Tests/RecordSift.Tests.csproj` | Non-SDK-style .NET Framework 4.8 NUnit assembly with a production project reference |
| `tests/RecordSift.Tests/packages.config` | Exact NUnit 3.14.0 and NUnit.ConsoleRunner 3.17.0 versions |
| `tests/RecordSift.Tests/BaselineAssemblyTests.cs` | NUnit tests for assembly load/copy, target framework and traditional assembly metadata |

NUnit 3.14.0 supports .NET Framework 4.8 (via its net45 assembly) and uses the MIT license. NUnit.ConsoleRunner 3.17.0 runs NUnit 3 assemblies and is MIT licensed. The runner is a test execution tool only; the production library intentionally has **no NuGet dependencies** in this issue. `packages.config` pins version numbers but is not a cryptographic lockfile. Review dependency vulnerability status and test runner upgrade options in issue #64 before release.

At this **historical initial baseline** milestone, NUnit smoke tests verified only that the solution was assembled; they did **not** verify FR/NFR acceptance. At that time 109 scenario rows were catalogued and Planned. Subsequent requirements-driven development added real CSV/XLSX import acceptance, contract fixtures and Windows quality gates; the [current traceability matrix](../testing/traceability.md) now tracks 108 active release scenarios with scenario-specific observed evidence. The retired NFR-009-SC02 cross-platform proposal is not a release 0.1 acceptance requirement.

## Evidence and limitations

The PR for issue #57 must link an observed Windows workflow run and report actual restore/build/test results before merge. The issue-57 PR smoke workflow checks that tests are **discovered** (at least three) and have no failures, rather than silently treating an empty test assembly as success. Issue #64 implements maintained release-branch Windows CI, publishing NUnit XML and NuGet audit logs as artifacts. The independent dependency-audit job checks exact pinned packages and fails on NuGet audit warnings NU1900–NU1905; a passing audit means no warnings were reported on that run, not that future vulnerabilities are impossible.

## Observed initial baseline evidence

Issue #57 was squash-merged after a successful Windows build and **3 of 3** NUnit baseline tests in [Actions run #41017804680](https://github.com/infoconex/RecordSift/actions/runs/41017804680). At that initial milestone the requirement scenarios were still Planned; assembly smoke tests did not certify CSV/XLSX behavior. Those scenarios were subsequently verified in release 0.1, as recorded in [traceability](../testing/traceability.md).

## Issue #85 CSV adapter addition

The initial issue #57 dependency inventory above is historical evidence; it is not the current total after CSV adapter integration. Issue #85 originally added the historical `src/RecordSift.CsvHelperAdapter` project and pinned CSV dependency graph; [ADR 0013](decisions/0013-physical-domain-application-infrastructure-assemblies.md) and [ADR 0014](decisions/0014-infrastructure-csv-adapter-naming.md) subsequently reorganized it as `RecordSift.Infrastructure.Csv`. See [ADR 0008](decisions/0008-csvhelper-isolated-adapter.md) for its original vendor-isolation decision and [PR #87](https://github.com/infoconex/RecordSift/pull/87) for observed verification.
