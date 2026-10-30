# ADR 0017 — Name Application input ports explicitly

**Status:** Accepted through [engineering issue #103](https://github.com/infoconex/RecordSift/issues/103). Supersedes the Application CSV input namespace specified in [ADR 0015](0015-layer-aligned-public-namespaces.md) without changing Domain or Infrastructure ownership.

## Context

The Application source contract currently lives under `RecordSift.Application.Abstractions.Csv`. Although that namespace is technically valid, *Abstractions* describes a C# technique rather than the architectural responsibility of this contract. The repository explicitly follows domain-centric ports-and-adapters design, and `ICsvRecordReader` is the input port consumed by the CSV import use case.

## Decision

- Move the four existing files from `src/RecordSift.Application/Abstractions/Csv/` to `src/RecordSift.Application/Ports/Csv/`.
- Change their public namespace to `RecordSift.Application.Ports.Csv` without renaming their types:
  `ICsvRecordReader`, `CsvReadOptions`, `CsvParseResult` and `CsvRecord`.
- Treat the options and source records/results as part of the input port contract, not as Domain business entities.
- Keep `RecordSift.Application.Importing.Csv` for orchestration and `RecordSift.Infrastructure.Csv.CsvRecordReaderAdapter` for the vendor-backed implementation.
- Update C# references, traditional MSBuild Compile paths, documentation, exact namespace reflection checks and architecture validation. Reject an accidental return of the legacy folder or namespace.

## Layer ownership

Domain owns schema, business invariants, diagnostics and public import results. Application owns technical input ports and import orchestration. Infrastructure implements ports using third-party parsing libraries and translates all results/errors into RecordSift-owned types. A port is owned by its consuming layer; moving a `TextReader`-based CSV capability into Domain would introduce IO concepts into business rules.

## Compatibility and verification

This pre-release namespace change is intentionally source- and binary-breaking for consumers that reference `RecordSift.Application.Abstractions.Csv`. They must replace their `using` directive with `RecordSift.Application.Ports.Csv` and rebuild. No compatibility aliases or duplicate interfaces are introduced.

This change preserves method signatures, validation, defaults, diagnostics, exceptions, parser output, safe limits, object ownership and NuGet dependencies. Verify using the existing Domain/Application core tests, real CSV adapter conformance, namespace reflection checks, architecture negative fixtures, Windows net48 build, Roslyn and documentation workflows. The full typed importer remains outside this issue.

## Alternatives

Retaining `Abstractions` offers no meaningful compatibility benefit before the first stable release and obscures intent. Naming the folder `Interfaces` separates its four closely related data contracts from their port. Moving the interface into Domain would incorrectly couple core business logic to CSV text IO. Creating more layers, interfaces, factories or generic reader abstractions is not justified by current use cases.
