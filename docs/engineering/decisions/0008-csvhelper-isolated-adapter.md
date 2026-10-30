# ADR 0008 — Isolate CsvHelper behind a RecordSift-owned CSV reader port

**Decision:** Replace the handwritten tokenizer with **CsvHelper 33.1.0** in a separate traditional net48 adapter assembly for [issue #85](https://github.com/infoconex/RecordSift/issues/85). Supersedes [ADR 0005](0005-bounded-csv-reader.md) and implements [ADR 0007](0007-domain-ports-and-adapters.md).

## Why CsvHelper 33.1.0

- The [official NuGet listing](https://www.nuget.org/packages/CsvHelper/33.1.0) explicitly supports **.NET Framework 4.8**, offers Apache-2.0 or MS-PL licensing, and lists its net48 runtime dependencies. The package and all transitives are pinned in the adapter's `packages.config`.
- CsvHelper **34.0.0**, published 2026-10-28, introduces substantial breaking APIs and changes malformed-quote defaults to recovery; see the [upstream migration guide](https://joshclose.github.io/CsvHelper/migration/v34.0.0.html). Deliberately adopt the stable 33.1 API for the historical baseline, with a future isolated upgrade and characterized behavior.
- [Sylvan.Data.Csv](https://www.nuget.org/packages/Sylvan.Data.Csv/) is a viable alternative with no direct netstandard2.0 dependencies; it is not selected for this issue because the previously scoped migration uses CsvHelper, and CsvHelper exposes direct raw-record parsing without an implicit header model. Re-evaluation is inexpensive because the adapter can be replaced independently.

## Architectural seam

- Core `RecordSift` defines `ICsvRecordReader.Read(TextReader, int)` and owns `CsvRecord`, `CsvParseResult` and safe `ImportDiagnostic` codes. **No CsvHelper assembly reference in the core project.**
- Infrastructure project `RecordSift.CsvHelperAdapter` references both the core project and CsvHelper. The caller composes `CsvTableReader.Read(recordReader, source, maxCharacters, schema, policy, positions)` using an injected reader.
- The adapter reads **already-decoded** CSV text. It does not infer headers, convert types, interpret cultural formats or choose encodings. Delimiters were added independently through [ADR 0009](0009-configurable-csv-delimiters.md) / FR-003.
- Malformed vendor parser exceptions are translated into `MALFORMED_SOURCE`; IO problems and budget overruns are source diagnostics. Raw values and vendor exception messages never escape.
- A bounded `TextReader` wrapper enforces the existing **caller-supplied maximum decoded characters**, counting an exact-limit EOF as valid. It implements no CSV grammar and leaves the caller's reader open.
- Existing records retain ordered fields, one-based source record and starting physical line, immutable snapshots, source-failure atomicity and headed/headerless policies.
- The domain-facing interface exposes **public construction/factories for neutral record/results** so a consumer can build a hand-written fake. No vendor mocks are necessary.

## Compatibility and limitations

This change intentionally breaks the **pre-release** helper signatures `CsvRecordReader.Read(source, limit)` and `CsvTableReader.Read(source, limit, schema, policy, positions)`: former is removed with the handwritten parser; latter now requires an explicit reader as its first parameter. Callers must reference `RecordSift.CsvHelperAdapter`, instantiate `new CsvHelperRecordReader()`, and pass it to `CsvTableReader.Read(reader, ...)`. No stable release 0.1 has been published; this break is recorded rather than silently introduced. Typed `ImportResult` and JSON contracts do not change.

The library remains pinned to net48 / C# 7.3. The `packages.config` graph must be audited on Windows, tested with the real parser, and assessed for assembly copy/binding. Adapter substitution tests do not certify parsing; existing and additional malformed/quoted/empty/budget tests verify real behavior. The user-facing typed FR-001/FR-004 acceptance remains Planned until the actual importer is implemented.

## Validation and evidence

The Red test commit [4dc2ab5](https://github.com/infoconex/RecordSift/commit/4dc2ab5327155b328fc57d6af5a0b15b2c4283ea) introduced consumer-owned fake port tests before the port existed. Windows run [#41078840074](https://github.com/infoconex/RecordSift/actions/runs/41078840074) failed expected compile error CS0246. **Observed Green:** [Windows run #41079918411](https://github.com/infoconex/RecordSift/actions/runs/41079918411) compiled the traditional net48 solution and passed **97/97 NUnit tests**, including the pre-existing parser scenarios and new fake-port tests. The pinned package inventory and NuGet vulnerability-audit job also succeeded. Initial Green attempt [#41079493328](https://github.com/infoconex/RecordSift/actions/runs/41079493328) exposed a physical-line regression (96/97), corrected by deriving the malformed field's line from CsvHelper metadata. Full importer acceptance remains independently Planned. [PR #87](https://github.com/infoconex/RecordSift/pull/87) records final evidence.
