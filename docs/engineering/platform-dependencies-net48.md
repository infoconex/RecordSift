# Release 0.1 platform-dependency review (NFR-009)

**Scope:** [NFR-009](../scenarios/non-functional/NFR-009.md), [issue #49](https://github.com/infoconex/RecordSift/issues/49), historical `.NET Framework 4.8`, C# 7.3, Windows release 0.1. This review supplies **NFR-009-SC01** acceptance evidence for the Windows-only release 0.1. The former cross-platform scenario NFR-009-SC02 was withdrawn from release scope; it is retained below solely as historical context, not as an open requirement.

## Reviewed dependency boundary

| Assembly | Direct dependencies / implementation | Platform finding |
| --- | --- | --- |
| `RecordSift.Domain` | .NET Framework `System` and `System.Core`; no project dependencies | Business schemas, diagnostics, validation and result contracts do not introduce Office, COM, native or OS-specific integration references. |
| `RecordSift.Application` | .NET Framework `System` and `System.Core`; RecordSift.Domain | Import orchestration and RecordSift-owned source/reader ports do not depend on implementation adapters or vendor packages. |
| `RecordSift.Infrastructure.Csv` | RecordSift.Application and Domain; pinned CsvHelper 33.1.0 and its managed NuGet dependencies | Format reader uses `TextReader`, `StreamReader`, encoding objects and explicit limits. No direct OS process/COM integration was identified in first-party source. |
| `RecordSift.Infrastructure.Xlsx` | RecordSift.Application and Domain; pinned ExcelDataReader 3.9.0; `System.IO.Compression` and `System.Data` | ZIP/OpenXML parsing uses framework streams and ExcelDataReader, not Excel automation, native Office executables or the Windows Registry. |
| `RecordSift.Infrastructure.Reporting` | RecordSift.Application and Domain; `System.Runtime.Serialization` and `System.Xml` | Framework serializers and formatters; no Office automation or platform process dependency identified. |

The review inspected the five production `.csproj` files, CSV/XLSX `packages.config` manifests, the production `.cs` source paths, the architecture validator and the Windows workflows. A search for direct P/Invoke, `Marshal`, `Process.Start`, Office interop/Excel COM, Registry, hardcoded `C:\\` paths, dynamic assembly execution and platform configuration APIs found no matching first-party production calls in the reviewed source. Dependency metadata and parser implementations were inspected separately; a text search alone is not proof of runtime portability.

## Existing executable evidence

- `scripts/validate_architecture_layers.py` enforces source/project ownership and inward dependency direction. The documentation workflow runs it on Ubuntu, which demonstrates that **this validation script** runs on that runner, not that the net48 library runs cross-platform.
- `.github/workflows/windows-net48.yml` builds and runs all five test assemblies on Windows 2022, checks the compiled public API baseline, runs the JSON regression fixtures, tests the real CSV/XLSX adapters, and enforces independent Domain/Application test dependencies.
- The NFR-003 Windows step verifies the runner has no Excel COM registration, executable or referenced Office automation code and runs two XLSX acceptance tests. NFR-002 additionally verifies CSV/XLSX imports while outbound connectivity is blocked.
- A reproducible source review can repeat a search over `src/**/*.cs` and production project metadata for `DllImport`, `Marshal`, `Process.Start`, `Microsoft.Office.Interop`, `Excel.Application`, `Microsoft.Win32`, `Registry`, `System.Management`, `C:\\` and similar Windows-specific APIs, then inspect every hit together with direct package and assembly references. Automated architecture and Office-free gates are complementary to that human dependency review.

## Result and boundaries

**NFR-009-SC01:** No *unnecessary first-party platform-specific integration* was identified in the release 0.1 import paths reviewed. The Windows-only `.NET Framework 4.8` target and Windows build/test tooling are intentional baseline requirements, not incidental library dependencies. Real adapters remain isolated behind RecordSift-owned ports. The review does not claim every transitive dependency is certified on other operating systems.

**Release 0.1 disposition:** NFR-009-SC01 has a repeatable manual platform-dependency review supported by successful Windows CI. This satisfies the Windows-scoped NFR-009 acceptance; issue #49 may be closed after the scope correction is merged and verified. The review does not certify other operating systems.

### Historical proposal — NFR-009-SC02 (retired, not an acceptance scenario)

Originally proposed as **"Verify modern cross-platform migration"**:

> **Given** a later portable target framework is introduced,  
> **When** the portable test matrix runs on the declared operating systems,  
> **Then** imports behave consistently without introducing an Excel or COM dependency.

This scenario was never executed and is **withdrawn from release 0.1 requirements** because Windows/.NET Framework 4.8 is the only approved deployment target. The scenario identifier is retained here for historical traceability and must not be reused. Future cross-platform support is **optional and uncommitted**; if approved later, create a separately scoped requirement rather than reviving this release gate.



**Change impact:** This review introduces no production code change, public API expansion, dependency replacement, target-framework change or new cross-platform promise.
