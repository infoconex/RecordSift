# ADR 0015 — Layer-aligned Domain and Application public namespaces

**Status:** Accepted through [engineering issue #99](https://github.com/infoconex/RecordSift/issues/99). Supersedes the temporary flat-namespace exception recorded in [ADR 0013](0013-physical-domain-application-infrastructure-assemblies.md); builds on [ADR 0014](0014-infrastructure-csv-adapter-naming.md).

## Context and pre-migration characterization

The last pre-migration release commit, [`879e630`](https://github.com/infoconex/RecordSift/commit/879e630f8b116eb00d0d3ed78a87aac5e527d56b), already had three separate CLR assemblies. However, **all 20 domain/application public type declarations still used `namespace RecordSift`**, making a type's purpose invisible in C# declarations, IntelliSense and API review. The CSV infrastructure implementation already used `RecordSift.Infrastructure.Csv.CsvRecordReaderAdapter`. This ADR captures the prior API surface explicitly and documents its intentional source/binary break; no existing release artifact is promised compatibility.

## Decision and complete source migration table

| Old public namespace/type names | New namespace | Types (class/interface/enum names unchanged) |
| --- | --- | --- |
| `RecordSift.DiagnosticCodes`, `DiagnosticScope`, `DomainMessageResources`, `ImportDiagnostic` | `RecordSift.Domain.Diagnostics` | `DiagnosticCodes`, `DiagnosticScope`, `DomainMessageResources`, `ImportDiagnostic` |
| `RecordSift.ExpectedColumn`, `ImportSchema`, `SchemaBindingResult`, `SourceColumnBinding`, `SourceColumnPosition` | `RecordSift.Domain.Schema` | `ExpectedColumn`, `ImportSchema`, `SchemaBindingResult`, `SourceColumnBinding`, `SourceColumnPosition` |
| `RecordSift.ImportResult`, `ImportStatus`, `ImportSummary` | `RecordSift.Domain.Results` | `ImportResult`, `ImportStatus`, `ImportSummary` |
| `RecordSift.ICsvRecordReader`, `CsvReadOptions`, `CsvParseResult`, `CsvRecord` | `RecordSift.Application.Abstractions.Csv` | `ICsvRecordReader`, `CsvReadOptions`, `CsvParseResult`, `CsvRecord` |
| `RecordSift.CsvDataRow`, `CsvHeaderPolicy`, `CsvTableReader`, `CsvTableResult` | `RecordSift.Application.Importing.Csv` | `CsvDataRow`, `CsvHeaderPolicy`, `CsvTableReader`, `CsvTableResult` |

Infrastructure is unchanged: `RecordSift.Infrastructure.Csv.CsvRecordReaderAdapter` implements the Application-owned port and privately uses CsvHelper. An application consumer now imports the appropriate namespaces explicitly:

```csharp
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Application.Importing.Csv;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;
```

This deliberately changes .NET full type names and source import statements. Existing compiled consumers must rebuild and change their `using` directives; no legacy type shims, type forwarding, or fake public interfaces are retained. This is approved before a stable 0.1 release.

## Invariants and architectural limits

- Domain has no IO, reader ports, vendor packages, or references to Application/Infrastructure. It owns diagnostics, messages, schema, and in-memory import results.
- Application owns narrow source input ports, neutral source record types/options, and CSV header/read orchestration. It references Domain only.
- Infrastructure references inward and translates CsvHelper outcomes into RecordSift-owned codes and contracts. Vendor exceptions or types are never public.
- The embedded diagnostic resource **manifest name remains `RecordSift.Resources.DomainMessages.resources`**, independent of new C# namespace and `RootNamespace`; invariant message templates and machine diagnostic string values are unchanged.
- No parser behavior, ownership/disposal, source limits, physical/logical row semantics, JSON contract, package versions, target framework or acceptance scenario is changed by this migration.
- C# 7.3 block-scoped namespaces and Microsoft PascalCase naming apply; no newer runtime or DI container is introduced.

## Automated verification and evidence

The Red architecture commit on [PR #100](https://github.com/infoconex/RecordSift/pull/100) requires exact source namespaces and project root namespaces, rejecting `namespace RecordSift` and wrong-layer declarations. Its negative fixtures verify that flat and outward declarations fail. The Green implementation updates production callers and both NUnit assemblies; `NamespaceOwnershipTests` verifies each of the 20 public type namespaces and rejects stray exported types outside their owning assembly. Windows .NET Framework 4.8 MSBuild, real CsvHelper contract conformance, core NUnit isolation, Roslyn IDE1006, package audit, resource manifest validation and documentation gates must pass on the final PR head before merge.

## Alternatives rejected

Keeping `namespace RecordSift` for source compatibility obscures ownership even after the CLR split. Adding compatibility facade types or duplicate public ports would weaken the intended clear domain model. Reorganizing import behavior or generalizing source ports now would mix new functionality with an otherwise testable API naming correction.
