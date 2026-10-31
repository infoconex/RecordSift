# Import result JSON contract — version 1.0

**Status:** Release 0.1 versioned report contract. In-memory Domain result models and CSV import are implemented. FR-035 provides an Infrastructure reporting adapter and executable JSON acceptance tests; the JSON Schema remains the authoritative wire definition. Full Draft 2020-12 meta-schema execution is not required by the current NUnit suite.  
**Related requirements:** [FR-028](../scenarios/functional/FR-028.md), [FR-030](../scenarios/functional/FR-030.md), [FR-035](../scenarios/functional/FR-035.md), [FR-036](../scenarios/functional/FR-036.md), [FR-037](../scenarios/functional/FR-037.md), [NFR-010](../scenarios/non-functional/NFR-010.md).  
**Machine-readable definition:** [`schemas/import-result.schema.json`](schemas/import-result.schema.json) (JSON Schema Draft 2020-12).

## Purpose and scope

This document defines the externally observable **JSON import report** for a single CSV or XLSX source. It does not define the internal .NET result type, accepted domain-record serialization, or an API implementation.

**Release 0.1 has a deliberately simple model:** a processed data row is either **accepted** or **rejected because of one or more errors**. There is **no warning severity**, `warningRowCount`, optional-field advisory mechanism or configurable severity in JSON contract version 1.0. A missing optional field is accepted unless some independently specified error-level validation rule applies. Configurable severity (FR-025) is a deferred Future idea, not a release gate or part of this contract.

## Top-level JSON fields

| Property | Type | Required | Meaning |
| --- | --- | --- | --- |
| `schemaVersion` | string, exactly `"1.0"` | Yes | JSON report contract version, independent of the package version |
| `sourceId` | nonempty string | Yes | Caller-provided opaque, non-sensitive source identifier; never a filesystem path or credential |
| `status` | `"Completed"` or `"Failed"` | Yes | Import outcome, independent of whether records were rejected |
| `message` | nonempty string | Yes | Human-readable success/failure summary; consumers must not parse its text for status |
| `summary` | object | Yes | Counts of distinct data rows |
| `diagnostics` | array | Yes | Individual error findings in deterministic order |

`Completed` means the importer finished reading and processing the source. It **may include rejected rows**. `Failed` means an unrecoverable source/schema/import failure occurred; its diagnostics must explain the failure. Cancellation and partial-processing status extensions are out of scope for version 1.0.

## Opt-in stopped outcome extension — JSON version 1.1 (FR-031)

Existing Completed/Failed reports continue to serialize as `schemaVersion: "1.0"`, preserving all seven frozen compatibility fixtures and the existing [v1.0 schema](schemas/import-result.schema.json). An opt-in fail-fast import that terminates validation at its first rejected record uses `schemaVersion: "1.1"` with `status: "Stopped"`, following the separately versioned [stopped-result schema](schemas/import-result-stopped.schema.json). No other fields, diagnostic codes or object shapes change. A stopped result includes the first rejected logical row, accurate prefix counts and its sanitized record diagnostic, and cannot be confused with a fully processed `Completed` result or fatal source-level `Failed` result. Version 1.1 is emitted only for the new stopped outcome in FR-031; cancellation (FR-032) remains separately tracked.

## Summary fields (all required)

| Field | Type | Meaning |
| --- | --- | --- |
| `processedCount` | nonnegative integer | Number of logical data records processed, excluding header rows |
| `acceptedCount` | nonnegative integer | Number of processed records meeting the configured schema/validation rules |
| `rejectedCount` | nonnegative integer | Number of distinct processed records with one or more error-level record diagnostics |

For a `Completed` import: **`processedCount = acceptedCount + rejectedCount`**. A record containing three errors contributes **one** to `rejectedCount` while all three separate error diagnostics remain available in `diagnostics`.

No `errorCount`, `warningCount`, `warningRowCount` or warning diagnostics are included. Source/schema errors do not count as rejected rows because no logical data row was rejected on their account. For a `Failed` import, summary counters describe any records actually processed before the failure; `status` and source-level diagnostics carry the failure classification.

## Diagnostic fields

Each element of `diagnostics` has these required properties:

| Property | Type | Meaning |
| --- | --- | --- |
| `scope` | `"Record"` or `"Source"` | A validation error on a data row, or a source/schema/import failure |
| `row` | positive integer or `null` | One-based logical data-record ordinal for `Record`; `null` for `Source` |
| `field` | nonempty string or `null` | Source field/column identifier if known |
| `code` | nonempty uppercase identifier | Machine-readable error code; registry to be fixed during implementation |
| `message` | nonempty string | Sanitized human-readable explanation |

Optional location fields: `worksheet` (nonempty name), `physicalRow` (positive XLSX row number), `physicalLine` (positive line number only when the CSV reader reliably provides it). Because quoted CSV fields can contain line breaks, logical data-record ordinal and physical line are different concepts.

There is no `severity` field in version 1.0: all diagnostics represent **errors**. A `Record` diagnostic must have a positive `row`; a `Source` diagnostic must use `row: null`. A missing source column may still be identified by `field` on a source-level diagnostic.

## Empty, completed-with-errors and failed imports

| Input condition | `status` | Counters | Diagnostic/message policy |
| --- | --- | --- | --- |
| Valid header-only CSV with no data records | `Completed` | All three summary counts zero | Informational message; no error for emptiness |
| Zero-byte CSV explicitly configured as headerless | `Completed` | All three summary counts zero | Informational message; no error for emptiness |
| Zero-byte CSV where headers are required | `Failed` | All three summary counts zero | Source error identifying missing required header row |
| Nonempty input missing required schema column | `Failed` | Counts of data rows actually processed, possibly zero | Source/schema error identifying missing required column |
| Source is fully processed but some data records fail validation | `Completed` | Accepted and rejected rows counted once each | Keep individual record errors; summary explains rejections |

Human-readable `message` wording is **not** a substitute for machine-readable `status` and error codes.

## Privacy, versioning and compatibility

- Never include raw source values, credentials, absolute file paths or stack traces by default.
- Callers should provide an opaque `sourceId`; the default JSON adapter redacts absolute filesystem paths and file URIs to `source-redacted`.
- The JSON Schema rejects unknown properties for contract version 1.0.
- The `schemaVersion` version is independent of RecordSift package and target-framework versions; incompatible public changes require deliberate schema versioning.
- Preserve deterministic diagnostics ordering for identical inputs and explicit settings.
- Serialize from the completed in-memory result; do not reopen the source file.
- JSON `message` is a trusted invariant status description selected from reporting resources; arbitrary caller-controlled result message strings are not copied into default reports.
- Any future warnings or other severities require a separately approved contract evolution and tests. They must not silently enter version 1.0.

## What JSON Schema cannot verify on its own

[The JSON Schema](schemas/import-result.schema.json) expresses required properties, types, enums, integer bounds, the `Record`/ `Source` row convention and rejection of unknown fields. It **cannot** by itself prove `processedCount = acceptedCount + rejectedCount`, distinct-row accounting, consistency between rejected rows and error diagnostics, privacy redaction or stable ordering.

When executable contract tests exist, they must assert these semantic invariants independently, not merely validate the JSON shape.

## Version-controlled compatibility fixtures

Seven approved JSON fixtures live in [the reporting integration tests](../../tests/RecordSift.Infrastructure.Reporting.Tests/Fixtures/Compatibility/) and cover successful, rejected, source-failed and empty imports, CSV physical-line provenance, XLSX worksheet/physical-row provenance, and missing XLSX worksheet failures. `JsonCompatibilityGoldenTests` compares complete parsed JSON trees (including required/null/omitted properties and diagnostic ordering) against these immutable examples and checks repeated serializer calls produce identical output. NUnit exercises the actual .NET Framework reporting adapter, not a mock.

Fixtures are **regression test inputs**, not normative JSON specifications; the JSON Schema remains the definitive wire contract. The fixture suite intentionally captures invariant English messages from existing resources only to maintain reviewable expected JSON; consumers still must not branch on human-readable messages. Future localization may require explicitly approved fixture changes. CI execution XML is never committed as a fixture.

## Remaining compatibility obligations

Baseline checks are enforced on .NET Framework 4.8 only. A future target-framework migration must compare the compiled public surface and representative golden JSON again, and separately examine assembly binding, target framework and package distribution changes; net48 baseline creation alone does not certify modernized target-framework compatibility.
