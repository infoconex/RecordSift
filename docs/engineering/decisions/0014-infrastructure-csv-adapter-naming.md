# ADR 0014 — Name the CSV infrastructure by its architectural role, not its parser vendor

**Additional implementation note:** [ADR 0021](0021-focused-csv-infrastructure-classes.md) separates the private character-limited reader and error classes into individual source files. The public adapter contract and assembly identity remain unchanged.

**Status:** Accepted in [engineering issue #97](https://github.com/infoconex/RecordSift/issues/97). Amends ADR 0013 and names introduced by ADR 0008.

## Context

The separate infrastructure assembly originally used `RecordSift.CsvHelperAdapter`, then briefly `RecordSift.Infrastructure.CsvHelper`. Both conflated the public architectural vocabulary with the particular vendor library. RecordSift owns `ICsvRecordReader`, while CsvHelper is only one implementation dependency. The user also requested avoiding the word `Helper` in names we control.

## Decision

1. Infrastructure projects and namespaces identify the **source format**: `RecordSift.Infrastructure.Csv`. An Excel implementation can be separately packaged under `RecordSift.Infrastructure.Excel`, retaining independent vendor packages and appropriate binary-reading ports.
2. Application owns `ICsvRecordReader`. The real parser implementation is `RecordSift.Infrastructure.Csv.CsvRecordReaderAdapter : ICsvRecordReader`. Only **concrete adapter implementations** use the `Adapter` suffix; no vendor types escape the application/domain contract.
3. Tests are packaged in `RecordSift.Infrastructure.Csv.Tests`; `CsvAdapterContractConformanceTests` derives from the neutral `CsvRecordReaderContractTests` fixture and verifies real CsvHelper behavior.
4. The external package/namespace `CsvHelper` and its exception/types retain their real names where used inside infrastructure. We do **not** rename third-party code. Avoid `Helper` in RecordSift-authored public class, namespace, and assembly identifiers.
5. All project references, solution paths, CI checks and documentation are updated. A binary/namespace breaking change is intentional and approved for the pre-release architecture correction; **diagnostic identifiers, resources, tests and import behavior remain unchanged**.

## Enforcement

`scripts/validate_architecture_layers.py` validates the new assembly, namespace and source placement; Windows net48 CI builds the projects, runs independently isolated core and real-adapter NUnit tests, and audits pinned third-party packages. Microsoft C# naming gates continue to apply.

## Verification

[PR #98](https://github.com/infoconex/RecordSift/pull/98) records observed documentation/architecture and Windows build/test results on the final head. Functional typed-import acceptance remains separately tracked.
