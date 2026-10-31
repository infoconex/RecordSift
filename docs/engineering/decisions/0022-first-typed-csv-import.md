# ADR 0022 — First typed CSV import vertical slice

**Status:** Accepted in engineering issue [#113](https://github.com/infoconex/RecordSift/issues/113), contributing to [FR-001 #2](https://github.com/infoconex/RecordSift/issues/2). This initial slice alone did not close FR-001 or FR-026; their complete acceptance was subsequently verified for release 0.1 (see [traceability](../../testing/traceability.md)).

## Context

The existing injected CSV parser and table binding service return neutral CSV records, but the previous surface did not produce a typed CLR result, an import summary, or informational messages for empty sources. A complete generalized mapping and validation framework would conflate FR-001 with FR-010, FR-011, FR-017 through FR-022 and FR-026.

## Decision

- Domain Results owns `TypedImportResult<T>`, an immutable collection snapshot of accepted typed records together with the existing `ImportResult` status, diagnostics, counts, an opaque caller-supplied source identifier and an outcome message. The accepted collection is immutable; individual caller-created objects are not deep-cloned.
- Application Importing.Csv owns `CsvImporter<T>`. Its constructor accepts a RecordSift-owned `ICsvRecordReader` port and an explicit caller-supplied `Func<IReadOnlyDictionary<string,string>,T>` conversion function. It delegates parser and source/header checks to the existing `CsvTableReader` and schema binding.
- Expected numeric/format conversion failures become safe, source-positioned row diagnostics and do not prevent subsequent records from being processed. Unexpected mapper exceptions remain programmer failures; defining a complete conversion/validator contract is separately tracked.
- Application-owned outcome texts live as `Outcome_*` values behind named accessors in `CsvMessageResources`; Domain-owned result precondition messages remain in `DomainMessageResources`. Machine-readable status and diagnostic codes never depend on text.
- All three NUnit assemblies retain inward dependencies. Application fake-port tests and real CsvHelper-backed integration tests exercise the same public use-case behavior, including quoted records, malformed input, empty source policy and accepted/rejected counts.

## Limitations and follow-up

This slice does not define generalized property binding, arbitrary object materialization policy, comprehensive validation, JSON serialization, direct filesystem/encoding APIs, or XLSX behavior. Full FR-001 acceptance scenarios retain Planned status until their broader requirements dependencies have been verified. No new vendor reference, framework dependency or general-purpose mapping abstraction is introduced.
