# RecordSift

**Import, validate, and report on structured data.**

RecordSift is an MIT-licensed Windows/.NET Framework 4.8 library for importing CSV and XLSX records, applying domain-owned validation, and returning structured diagnostics and reports. **Stable version 0.1.0 is publicly available on [NuGet.org](https://www.nuget.org/packages/RecordSift/0.1.0).**

## Why use RecordSift?

**Reading a CSV file or Excel worksheet is only the first step of a business-data import.** Applications also need to match incoming columns to their data model, convert values, validate each record, decide which rows to accept, and explain rejected data. RecordSift supplies a **consistent CSV/XLSX import-and-validation workflow** so applications do not need to reinvent those surrounding steps for every file format.

Choose RecordSift for Windows/.NET Framework 4.8 applications that need:
- **Typed import and explicit mapping:** declare expected columns and map them to your own writable C# properties with culture-aware conversion.
- **Reusable validation:** apply required-value, length, range, duplicate, pattern and custom-record checks.
- **Useful outcomes:** accept valid records, reject problematic rows, and retain deterministic diagnostic codes with field and row locations.
- **Optional reports:** inspect the typed result in memory or produce JSON, readable text or machine-interchange diagnostic CSV.
- **Local processing with configurable resource budgets:** no Excel installation or runtime service; callers remain responsible for choosing sensible limits.

**When not to use it:** for a simple parse-only task, direct CsvHelper or ExcelDataReader usage may be enough—RecordSift builds on those libraries and adds a higher-level pipeline, not a new parser. Release 0.1.0 is Windows/net48-only, synchronous and potentially eager; it is **not** intended for modern .NET-only/cross-platform applications, true streaming, or strict process memory/CPU ceilings. It does not automatically save accepted records or serialize them as data JSON. See the [working import example and decision guide](docs/usage/getting-started.md#why-use-recordsift).

## Start using RecordSift

**[Getting started: install RecordSift 0.1.0, run a complete CSV example, import XLSX, and inspect errors](docs/usage/getting-started.md)**

In **Visual Studio on Windows**, create a **Console App (.NET Framework)** targeting **.NET Framework 4.8**. Install the package using **Manage NuGet Packages**, or in Package Manager Console:

```powershell
Install-Package RecordSift -Version 0.1.0
```

RecordSift is a library called from your own C# application. It does not provide a desktop UI or a standalone file-conversion command. The [full C# 7.3 walkthrough](docs/usage/getting-started.md) contains a complete, runnable `Program.cs`, then explains CSV headers, XLSX worksheet selection, mapping/validation, accepted versus rejected rows, JSON/text/CSV reports, and troubleshooting. The snippets below illustrate specific API calls and **reuse variables** declared earlier; paste the complete program from the walkthrough when starting from an empty project.

**Published release:** [RecordSift 0.1.0 on NuGet.org](https://www.nuget.org/packages/RecordSift/0.1.0) · [GitHub v0.1.0 release](https://github.com/infoconex/RecordSift/releases/tag/v0.1.0) · [consumer guide](docs/release/0.1-consumer-guide.md). The GitHub tag points to the original published source commit `2db5c86bc26010afcb83c2e455d2471d8c9340d6`. [Release governance #199](https://github.com/infoconex/RecordSift/issues/199) records package checksum and successful public-feed consumer verification. Later documentation changes do not alter the immutable 0.1.0 binaries.

## Public API quick start (.NET Framework 4.8)

For a complete C# 7.3 `Program.cs`, see the **[Getting Started guide](docs/usage/getting-started.md)**. It uses the shared **10-column customer schema** with clear named `ExpectedColumn` and `SourcePropertyMapping` arguments, commented local budgets, 20 complete valid customer records, and an equivalent Excel import. Download [customers.csv](docs/samples/customers.csv), [customers.xlsx](docs/samples/customers.xlsx), [invalid CSV](docs/samples/customers-invalid.csv), or [invalid Excel](docs/samples/customers-invalid.xlsx).

RecordSift's public API requires caller-chosen positive CSV decoded-character and XLSX compressed-byte budgets. For readability, the tutorial defines these as local variables, **not `App.config` settings**. The current mapping API requires explicit source-to-property mapping declarations even when the names are identical. A potential convenience API is tracked in [issue #254](https://github.com/infoconex/RecordSift/issues/254) and is **not implemented**.

## Results and JSON reports

CSV and XLSX imports return a typed `TypedImportResult<T>`, not a JSON string. Use `AcceptedRecords` for typed customers, and `Status`, `Summary`, and `Diagnostics` to interpret outcomes and errors. `JsonImportResultSerializerAdapter` provides an **optional JSON outcome/diagnostic report**, which intentionally omits customer record data. See the [reporting example](docs/usage/getting-started.md#optional-json-and-readable-reports) and [JSON contract](docs/specifications/import-result.md).

## XLSX input security boundary

The XLSX reader independently checks ZIP entry count (512 maximum), both declared and actually decompressed package bytes (the smaller of 64 MiB or 32 times the caller's compressed-byte budget), and shared-string capacity hints and item counts (one million maximum). XML inspected during shared-string preflight prohibits DTDs and external entity resolution. These checks run before ExcelDataReader 3.9.0 parses the workbook, in addition to caller-controlled row/column/cell limits. The read is synchronous and eagerly buffers data; these safeguards do **not** guarantee a fixed maximum process heap, interrupt an expensive parse immediately on cancellation, or establish that all malformed files are harmless. Set conservative input budgets for untrusted XLSX. See the [release security assessment](docs/engineering/release-0.1-security-assessment.md).

## Diagnostic CSV security boundary

`CsvImportResultSerializerAdapter` produces deterministic **machine-interchange CSV**, not a spreadsheet-safe export. Caller-controlled source, field and worksheet identifiers are preserved as original text. CSV quoting handles delimiters and line breaks but does not prevent applications such as Excel from treating values beginning with `=`, `+`, `-` or `@` as formulas. Do **not** directly open diagnostic exports containing untrusted metadata in spreadsheet software; import columns explicitly as text or apply a separately tested spreadsheet-specific transformation. This library does not silently alter metadata to provide spreadsheet formula protection. See the [FR-038 report contract](docs/scenarios/functional/FR-038.md) and [consumer safety guidance](docs/release/0.1-consumer-guide.md).

## Development baseline

- [Release-candidate readiness assessment](docs/engineering/release-0.1-readiness.md)
- [Published 0.1.0 consumer guide](docs/release/0.1-consumer-guide.md)
- [0.1.0 release notes and known limitations](docs/release/0.1-release-notes.md)
- [Historical release verification and promotion checklist](docs/release/0.1-release-checklist.md)
- [Stable 0.1.0 publication and public-feed verification procedure](docs/release/0.1-publication-procedure.md)
- [Dependency and licensing inventory](docs/release/0.1-dependencies.md)
- [MIT project license](LICENSE) and [third-party notices](THIRD-PARTY-NOTICES.md)
- [Historical net48 build and test instructions](docs/engineering/baseline.md)
- [Requirements, specifications, and engineering documentation](docs/README.md)
- [Functional requirements](docs/requirements/functional.md)
- [Nonfunctional requirements](docs/requirements/non-functional.md)
- [Acceptance scenario specifications](docs/scenarios/README.md)
- [Architecture and dependency direction](docs/engineering/architecture.md)

## Development workflow

- `main` is reserved for accepted releases.
- `release/0.1` is the integration branch for the 0.1 release.
- Each tracked issue uses an independent short-lived branch, reviewed and squash-merged into `release/0.1`.
- Scenarios remain Planned until executable acceptance evidence exists in [traceability](docs/testing/traceability.md).
- Future framework and package modernization must establish separate compatibility evidence before being declared complete.
