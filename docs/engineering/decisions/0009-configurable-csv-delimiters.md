# ADR 0009 — Configurable CSV delimiters at the reader port

**Decision:** Implement [FR-003](../../scenarios/functional/FR-003.md) in [issue #10](https://github.com/infoconex/RecordSift/issues/10) using a vendor-neutral `CsvReadOptions` value object and the existing `ICsvRecordReader` port. CsvHelper's delimiter setting remains an adapter implementation detail.

## Contract and behavior

- The delimiter defaults to comma when options are omitted. An explicit `new CsvReadOptions(delimiter)` requests a specific delimiter without changing header or schema binding.
- A valid delimiter is **exactly one** non-whitespace, non-control, non-surrogate UTF-16 character, **except tab (U+0009) is supported**. Double quotation mark (`"`) is reserved for CSV quoting. Examples: `;`, `|`, `^`, `\t`, and `,`. Empty, null, multicharacter, space, CR, LF, NUL, control characters, and quote characters are rejected.
- `CsvReadOptions` stores the caller's requested delimiter and exposes `IsValid`; the core's `CsvTableReader` checks configuration **before calling the input port**. The real CsvHelper adapter also validates options before reading any source data when used directly.
- Invalid configuration returns a **source-scoped** `INVALID_CSV_DELIMITER` diagnostic with a fixed safe message. Failed raw parse/table results contain no partial records or rows. Raw delimiter text and source data are never interpolated into diagnostics.
- `ICsvRecordReader.Read(TextReader, int, CsvReadOptions options = null)` carries the neutral options. CsvHelper alone translates them into its parser configuration; the core assembly and test fakes do not reference CsvHelper.
- Delimiter is **independent of numeric/date culture**. Auto-detection, multi-character delimiters and format guessing are out of scope. Character-budget limits, strict malformed CSV errors, quoted delimiters, source provenance, explicit header policy, and `TextReader` ownership remain unchanged.
- The interface signature adds an optional third argument. Pre-0.1 custom `ICsvRecordReader` implementations must add the optional `CsvReadOptions` parameter to compile; normal two-argument calls continue to compile when using the updated interface.

## Design considerations

The input port, rather than an adapter constructor or ambient/global configuration, owns the delimiter option: the caller can deterministically configure a single read, and tests can capture or fake the same contract. This avoids coupling consumer code to CsvHelper configuration models. Validating again in the adapter also protects direct port calls against ambiguous settings.

A configuration error is not a row-validation error: no record has been processed and no source data should be consumed.

## Observed test evidence

[Red commit](https://github.com/infoconex/RecordSift/commit/c4b7a92b67a0e9d57c702aeee261087fee1e793a) was intentionally uncompilable because `CsvReadOptions` did not exist; [Windows #41081023227](https://github.com/infoconex/RecordSift/actions/runs/41081023227) observed CS0246 and incompatible fake port. [Green Windows #41081248991](https://github.com/infoconex/RecordSift/actions/runs/41081248991) compiled the historical net48 projects and executed **112/112 passing NUnit tests**, with NuGet dependency audit passing. [PR #88](https://github.com/infoconex/RecordSift/pull/88) tracks final checks.

The FR-003 scenario tests currently exercise real CsvHelper raw reader/table integration, **not a complete typed importer**. Both top-level FR-003 acceptance scenarios remain Planned in the traceability matrix until the public typed import API is available.
