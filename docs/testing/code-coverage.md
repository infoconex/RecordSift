# Code coverage

RecordSift measures **line and branch coverage** as a diagnostic for the five production assemblies. Coverage complements requirements-to-scenario traceability, assertion quality, contract tests and security-focused tests; it does not replace them. Issue [#271](https://github.com/infoconex/RecordSift/issues/271) tracks this engineering infrastructure.

## Scope

Coverage is collected using Microsoft's pinned `dotnet-coverage` tool (18.12.0) and converted into human-readable reports using ReportGenerator (5.5.12). Both are local .NET CLI tools in [`.config/dotnet-tools.json`](../../.config/dotnet-tools.json), **not** production NuGet/package dependencies. Configuration in [`config/coverage.xml`](../../config/coverage.xml) includes these production binaries:

| Layer | Assembly |
| --- | --- |
| Domain | `RecordSift.Domain` |
| Application | `RecordSift.Application` |
| CSV adapter | `RecordSift.Infrastructure.Csv` |
| XLSX adapter | `RecordSift.Infrastructure.Xlsx` |
| Reporting | `RecordSift.Infrastructure.Reporting` |

The include-only assembly filter excludes test projects, third-party parser dependencies and unrelated runtime modules. No source files are excluded by a custom coverage configuration. The collector's built-in defaults may exclude compiler-generated or debugger-marked methods. Coverage validation requires all five production assemblies, line and branch instrumentation, and source file paths that resolve to RecordSift sources.

## Run locally

**Environment:** Windows, .NET Framework 4.8 developer targeting pack, Visual Studio MSBuild/Build Tools, NuGet CLI, PowerShell 7, and a .NET SDK capable of running the pinned tools (the verified Windows run used SDK 10.0.403 and `dotnet-coverage` on .NET 8.0.33). Existing `packages.config` project structure, .NET Framework target, C# 7.3 and the NUnit ConsoleRunner are unchanged.

From the repository root:

```powershell
nuget restore RecordSift.sln -NonInteractive -PackagesDirectory packages -ConfigFile NuGet.Config
msbuild RecordSift.sln /m /t:Rebuild /p:Configuration=Release "/p:Platform=Any CPU"
.\scripts\run_coverage.ps1
```

The script restores pinned .NET tools, runs [`scripts/run_tests.ps1`](../../scripts/run_tests.ps1) under coverage collection, verifies full NUnit execution and the five production coverage packages, then generates reports. Coverage collection always runs the **full five-assembly suite** so measurements are comparable. For targeted test execution without coverage, use `scripts/run_tests.ps1` with its existing `-Level`, `-Verification`, `-Requirement`, `-Scenario`, and `-Where` selectors. Targeted test execution does not replace the full coverage run.

Generated files (all ignored through the existing `TestResults/` rule):

- `TestResults/nunit-selected.xml`: NUnit execution evidence.
- `TestResults/coverage/coverage.cobertura.xml`: original collector result, suitable for CI integrations.
- `TestResults/coverage/report/index.html`: browseable source coverage.
- `TestResults/coverage/report/SummaryGithub.md`: GitHub Actions job summary.
- `TestResults/coverage/report/Cobertura.xml`: transformed/merged report.

The `windows-net48.yml` workflow runs a separate coverage job on Windows, displays the report summary in GitHub Actions and uploads the full report and NUnit XML as a downloadable artifact. Collector or test failures, absent production assemblies, missing source mapping, and invalid coverage output fail that job. **Coverage percentage alone does not fail CI**.

## Initial measured baseline

Measured on **November 12, 2026**, using the full existing test suite. Production source baseline was `main` at `1809d06c0fade59e4769d16e0f8eaf10fa426004`, with coverage instrumentation on PR [#272](https://github.com/infoconex/RecordSift/pull/272) at head `d9e21ffd10842d43975095812ed85097da09a8e4`.

**Evidence:** [Windows run #43261247003](https://github.com/infoconex/RecordSift/actions/runs/43261247003), coverage job success and artifact `net48-code-coverage-43261247003-1`. NUnit: **419 discovered, 418 passed, 0 failed, 1 explicitly skipped**, across five test assemblies.

| Production assembly | Line coverage | Branch coverage |
| --- | ---: | ---: |
| Domain | 95.69% | 89.16% |
| Application | 89.57% | 79.46% |
| Infrastructure.Csv | 84.55% | 79.55% |
| Infrastructure.Xlsx | 92.67% | 81.75% |
| Infrastructure.Reporting | 94.50% | 91.67% |
| **Overall, weighted** | **92.49%** | **85.08%** |

Observed toolchain: GitHub `windows-2022`, .NET SDK `10.0.403`, MSBuild `17.14.60`, NuGet CLI `7.9.0.83`, PowerShell `7.6.9`, collector `18.12.0` (net8 runtime `8.0.33`), ReportGenerator `5.5.12`. This is evidence of the Windows .NET Framework 4.8 configuration, not other platforms or target frameworks.

## Interpreting the measurements

A line marked covered means it was executed during the selected tests. Branch coverage counts decisions exercised; neither number establishes that assertions are sufficient or that all failure paths and security boundaries are protected. Review uncovered paths using the HTML report, prioritizing parser error handling, import limits, and negative behaviors. Be aware that generated/compiler-transformed code and optimization can affect instrumentation and comparisons.

Future PRs may compare these baselines, but **do not impose an arbitrary overall or per-assembly percentage gate**. Any coverage threshold or modified-code enforcement policy requires a separately reviewed decision, based on meaningful baseline measurements and exclusions.

## Microsoft-only reporting evaluation (November 12, 2026)

This evaluation was conducted on the unmerged [PR #272](https://github.com/infoconex/RecordSift/pull/272); it does **not** authorize replacing ReportGenerator or merging the PR.

- **Method information is already present.** The Cobertura XML from the successful original full-suite [Windows run #43262630230](https://github.com/infoconex/RecordSift/actions/runs/43262630230) contains `<methods><method ... line-rate="...">` elements for **236 instrumented methods across five production assemblies**; **228** had nonzero line coverage (**96.61%** executed) and **8** had zero. A method-level execution summary can therefore be calculated from the **existing collector output without a PRO license or another collector**. This metric counts measured methods that were entered at least once; it is *not* the same as line or branch coverage and should not be mistaken for a correctness guarantee. The same XML has **186** methods with 100% method-level *line* coverage, which does not mean all branches were exercised.
- **Original line and branch data is preserved by direct Cobertura collection.** The original artifact contains **1,540/1,665 covered lines** and **975/1,146 covered branches** (92.49% line, 85.08% branch).
- **Native file and Microsoft XML were generated successfully, with a limitation.** In [Windows experiment #43266584092](https://github.com/infoconex/RecordSift/actions/runs/43266584092), one full NUnit execution (419 total, 418 passed, 1 skipped) produced `coverage.coverage` and converted Microsoft XML/Cobertura via `dotnet-coverage merge`. Microsoft XML contained **236 function entries**, with **228** showing executed code. However, converted Cobertura had **no branch counts/branch detail** (`branches-valid` missing, and every branch rate reported as 1). Our validation correctly rejected this result, so it must **not** replace the direct-Cobertura baseline. The original tests were successful; the experimental coverage job failed because branch reporting was incomplete.
- **IDE import is documented, not yet executed.** Microsoft documents importing `.coverage` in the Visual Studio Code Coverage Results window and confirms code coverage is available in Visual Studio 2026 Community/Professional. This project evaluation did **not** verify an actual IDE import or whether the local user's installed Visual Studio is 2026. Source-level local IDE experience remains an optional manual follow-up.
- **HTML reporting remains a separate concern.** Microsoft's `dotnet-coverage` produces coverage formats and conversion but does not offer a standalone HTML report equivalent to ReportGenerator. ReportGenerator's free edition still provides source-highlighted HTML line/branch coverage; its method-summary presentation is sponsor-only. We have **not** established a Microsoft-only replacement for equivalent standalone HTML.
- **Decision recommendation:** Keep direct Cobertura collection and the existing free HTML output for now. If displaying method-level numbers in GitHub Actions is desirable, evaluate a small summary derived from `<method line-rate>` in the already-produced Cobertura data, preserving the original collector output and avoiding additional paid or third-party tools. Any reporter removal or other tooling change needs separate approval.

## Tool references

- [Microsoft: dotnet-coverage](https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-coverage)
- [Microsoft: coverage settings](https://github.com/microsoft/codecoverage/blob/main/docs/configuration.md)
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator)
