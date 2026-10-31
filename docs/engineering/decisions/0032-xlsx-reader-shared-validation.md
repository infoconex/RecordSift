# ADR 0032: XLSX reader port and shared typed record validation

**Status:** Accepted and implemented for FR-002 in release 0.1; Windows CI acceptance subsequently passed (see [traceability](../../testing/traceability.md)).
**Issue:** [#6](https://github.com/infoconex/RecordSift/issues/6)

## Context

CSV imports currently combine source-specific reading and schema-binding with typed record validation. XLSX adds binary parsing and physical worksheet provenance but must not duplicate business rules or expose third-party types in Domain/Application.

## Decision

Extract the existing typed validation and result accounting into Application `RecordValidationPipeline`, accepting immutable, format-neutral `ImportDataRow` values. Keep `CsvImporter<T>` as the CSV-specific orchestration facade. Add `XlsxImporter<T>` with a narrow `IXlsxWorksheetReader` port and immutable XLSX record/read-result contracts. The workbook adapter alone uses ExcelDataReader 3.9.0 (MIT license, .NET Framework 4.6.2 compatible) and framework ZIP handling; its NuGet dependencies are pinned and audited.

A workbook is read atomically, defaults to its first worksheet, and its first nonblank physical row supplies headers. Blank worksheet rows are skipped, missing cells map to empty text, numeric and date values are formatted invariantly, and cached formula values are used without recalculation. Unsupported/corrupt source content fails via trusted diagnostic codes. Input streams remain caller-owned. Workbook byte limits, worksheet row/column/cell limits, and ZIP entry/advertised expansion limits mitigate untrusted-resource risks. Explicit worksheet selection belongs to FR-005.

## Consequences

Existing Domain schema binding, required/optional, length, ranges, custom and duplicate validation, typed results, and reporting adapters are reused. The new parser dependency remains replaceable. XLSX diagnostics carry worksheet and physical-row coordinates. CSV behavior retains its public API and is protected by the full CSV regression suite.

Declared ZIP expansion checks cannot guarantee safety against every malicious archive; a future hardened parser/resource-budget evaluation remains possible. Do not use Office COM, execute formulas, or resolve external links.
