# ADR 0031 — Invariant, resource-backed readable import reporting

**Status:** Accepted for release 0.1  
**Issue:** [#38](https://github.com/infoconex/RecordSift/issues/38)  
**Requirement:** [FR-037](../../scenarios/functional/FR-037.md)

## Decision

Application owns the `IImportResultFormatter` port. The concrete `ReadableImportResultFormatterAdapter` belongs to `RecordSift.Infrastructure.Reporting` and formats existing `TypedImportResult<T>` statuses, Domain counters, diagnostic codes, sanitized messages, scopes, record and field identifiers and available physical provenance. It never imports a source or changes Domain validation classification. An error count in presentation is simply the number of structured diagnostic entries; rejected rows continue to be counted once by Domain.

The release 0.1 human-readable format is **invariant English**, with all fixed templates stored in the reporting adapter's `.resx` catalog and numbers formatted using `CultureInfo.InvariantCulture`. Output is stable regardless of the caller's current culture. The format is not a versioned machine-readable API; consumers must use structured status/diagnostic codes for programmatic decisions.

Completed zero-row imports expressly report that no data records were found, whereas fatal source failures are described as failures with their existing source diagnostics. Each rejected row is identified by logical record number, optional declared field identifier and diagnostic code/reason. Reliable physical source location is appended when available. Multiple diagnostics on one record remain distinct in readable detail.

To protect privacy, the formatter does not echo caller-provided `TypedImportResult<T>.Message`, accepted-record objects, or `SourceId` (which may be an absolute path). Diagnostic text originates from the sanitized Domain registry, and record values are not interpolated. Explicit localization/culture customization is deferred: adding languages can extend the resource catalog without changing Domain behavior or JSON contract 1.0.

## Consequences

JSON output and readable text are independent Infrastructure presentations of a single immutable Domain result. The formatter is replaceable behind an Application port. Tests verify counts, no-data success versus missing-header failures, row/field context, privacy and invariant culture. New formatting requirements must not move business logic into the reporting adapter.
