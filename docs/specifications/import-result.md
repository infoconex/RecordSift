# Import result JSON contract — version 1.0 (proposed)

**Status:** Specification only. The .NET import API, JSON serialization code, and executable contract tests have **not** been implemented.  
**Related requirements:** [FR-028](../scenarios/functional/FR-028.md), [FR-030](../scenarios/functional/FR-030.md), [FR-035](../scenarios/functional/FR-035.md), [FR-036](../scenarios/functional/FR-036.md), [FR-037](../scenarios/functional/FR-037.md), [NFR-010](../scenarios/non-functional/NFR-010.md).  
**Machine-readable definition:** [`schemas/import-result.schema.json`](schemas/import-result.schema.json) (JSON Schema Draft 2020-12).

## Purpose and boundaries

This document specifies the **externally observable validation report JSON**, not the internal .NET record type, the list of accepted domain objects, a database model, or a .NET build artifact. A caller should be able to inspect completion status, data-row counts, and sanitized diagnostics without reopening a CSV or XLSX file.

The initial contract describes **one source per report**. It is intentionally strict about property names and types. Expected result examples will be committed as **test fixtures** when executable contract tests are developed, rather than copied into this specification.

## Top-level JSON fields

| Property | Type | Required | Meaning |
| --- | --- | --- | --- |
| `schemaVersion` | string, exactly `"1.0"` | Yes | JSON report contract version, independent of package or .NET version |
| `sourceId` | nonempty string | Yes | Caller-controlled, non-sensitive source identifier; not an absolute file path |
| `status` | `"Completed"` or `"Failed"` | Yes | Machine-readable import outcome |
| `message` | nonempty string | Yes | Human-readable explanation, never a substitute for `status` or diagnostic codes |
| `summary` | object | Yes | Distinct data-row counts |
| `diagnostics` | array | Yes | Individual record-level or source-level findings, in deterministic order |

`Completed` means import processing **finished**, not that every record was accepted. A completed import may have rejected rows and error-level *record diagnostics*. `Failed` means import was terminated by an unrecoverable source, configuration or schema failure. There must be at least one corresponding source-level error diagnostic for a Failed outcome.

Cancellation (`FR-032`), streaming partial results, and other future states are not part of version 1.0; introducing them requires documented schema versioning and tests.

## Summary fields (all required)

| Field | Type | Semantics |
| --- | --- | --- |
| `processedCount` | nonnegative integer | Number of logical data records actually processed; header rows excluded |
| `acceptedCount` | nonnegative integer | Distinct processed rows accepted, including warning-only rows |
| `rejectedCount` | nonnegative integer | Distinct processed rows with one or more **error-level record diagnostics**, counted **once per row** |
| `warningRowCount` | nonnegative integer | Distinct processed rows with one or more **warning-level record diagnostics**, counted **once per row** |

For a `Completed` import, `processedCount = acceptedCount + rejectedCount`. Always `warningRowCount <= processedCount`; warning-bearing rows can also be rejected, so **do not** add `warningRowCount` to `acceptedCount + rejectedCount`. A row with multiple error-level diagnostics contributes only one to `rejectedCount`. A row with multiple warnings contributes only one to `warningRowCount`.

For `Failed` imports, the counters describe only rows actually processed; a failure before any data row can have all counters set to zero. A source error is not a rejected data row. The distinction between successful empty input and failed missing-header input must be visible in `status` and `diagnostics`.

**No ambiguous `errorCount` or `warningCount` property is included in this contract.** The count of individual findings can be derived from `diagnostics` as needed; any future persisted aggregate needs an explicitly distinct name such as `errorDiagnosticCount`.

## Diagnostic fields

Each item in `diagnostics` has the following required properties:

| Property | Type | Meaning |
| --- | --- | --- |
| `scope` | `"Record"` or `"Source"` | Classifies a data-row finding versus a source/schema/import failure |
| `row` | positive integer or `null` | One-based **logical data-record** ordinal for `Record`; `null` for `Source` |
| `field` | nonempty string or `null` | Failing field/column identifier if known; otherwise `null` |
| `severity` | `"Error"` or `"Warning"` | Severity of this individual finding |
| `code` | uppercase machine-readable code | Stable error category; the complete code registry will be established during implementation |
| `message` | nonempty string | Human-readable, sanitized reason |

Optional diagnostic location fields are `worksheet` (nonempty string), `physicalRow` (positive integer) and `physicalLine` (positive integer). `physicalLine` may be supplied for CSV only when reliably reported by the parser; multiline quoted CSV records make physical line numbers different from logical record ordinals.

For a `Record` diagnostic, `row` is a positive integer. For a `Source` diagnostic, `row` is `null`; `field` may still name a missing required column. File/schema failures do not increment `rejectedCount` or `warningRowCount`.

For example, a configured optional-phone warning may use code `OPTIONAL_VALUE_MISSING`; **missing optional values do not cause warnings automatically**. These example diagnostic codes are not yet a frozen registry.

## Zero-row and failed-import behavior

| Input condition | `status` | Summary | Diagnostic policy | Message meaning |
| --- | --- | --- | --- | --- |
| CSV containing valid headers and zero data records | `Completed` | All counts zero | No error or warning for emptiness alone | No data records found |
| Zero-byte CSV explicitly configured as headerless | `Completed` | All counts zero | No error or warning for emptiness alone | Source contains no records |
| Zero-byte CSV when headers are required | `Failed` | All counts zero | Source-level error, `row: null` | Required header row is missing |
| Nonempty input missing a required declared source column | `Failed` | Actual processed rows (possibly zero) | Source-level/schema diagnostic | Identifies missing column |
| Mixed valid/invalid data rows, but source processing completes | `Completed` | Count each accepted/rejected row once | Retain each individual record diagnostic | Processing finished with rejected records |

Human-readable `message` text is **not** an invariant snapshot contract by itself. Localized or improved copy must not change the meaning of `status`, the diagnostic codes or the counters. Tests should check meaning; exact text assertions should be limited to explicitly frozen messages, if any.

## Privacy and compatibility

- Do not serialize raw customer values, exception stack traces, absolute filesystem paths, credentials or other sensitive input by default.
- Use a caller-controlled `sourceId` rather than leaking the real source path.
- All JSON objects reject undocumented fields for version 1.0 (`additionalProperties: false`).
- `schemaVersion` is mandatory; changing field names, status values, interpretation or allowed values must follow a deliberate compatible evolution or version bump.
- All diagnostics retain deterministic order given identical input and explicit options.
- JSON reporting operates on the **existing** result object, not a second pass over source data.

## What JSON Schema does and does not guarantee

The [JSON Schema](schemas/import-result.schema.json) validates structural constraints: required properties, data types, enum values, positive/zero bounds, record-versus-source row location, and unknown-property rejection.

**Executable contract tests are still required** for relationships that ordinary JSON Schema cannot express portably: `processedCount = acceptedCount + rejectedCount`, uniqueness of counted rows, warning/rejected overlap, semantic source-level failure classification, sensitive-data redaction and deterministic output. A JSON document passing structural validation is not automatically semantically correct.

## Future version-controlled test fixtures (not yet created)

When the relevant .NET contract-test project is implemented, keep **expected JSON output files** alongside tests under `tests/RecordSift.ContractTests/Fixtures/`, using names such as:

- `successful-import.json`
- `completed-with-errors.json`
- `completed-with-warnings.json`
- `empty-import.json`
- `failed-import.json`

These are planned fixture paths, **not existing files or passing tests**. Each fixture should be linked to the relevant `FR-###-SC##` acceptance scenario. Contract tests must deserialize and validate generated JSON against the schema and these expectations; they must also verify row-count invariants directly. Actual test-run outputs, coverage results and temporary reports should remain CI execution artifacts, not committed golden fixtures.

## Deferred decisions

Choose the initial public .NET result API, exact diagnostic code registry, test framework/JSON Schema validator compatible with the target frameworks, and API migration/versioning policy during their own implementation issues. Do not conflate the JSON report version (`schemaVersion`) with the application's release version.
