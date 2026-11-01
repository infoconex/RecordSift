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

RecordSift is a library called from your own C# application. It does not provide a desktop UI or a standalone file-conversion command. The [full C# 7.3 walkthrough](docs/usage/getting-started.md) starts with a complete, runnable `Program.cs`, then explains CSV headers, XLSX worksheet selection, mapping/validation, accepted versus rejected rows, JSON/text/CSV reports, and troubleshooting. The snippets below illustrate specific API calls and **reuse variables** declared earlier; paste the complete program from the walkthrough when starting from an empty project.

**Published release:** [RecordSift 0.1.0 on NuGet.org](https://www.nuget.org/packages/RecordSift/0.1.0) · [GitHub v0.1.0 release](https://github.com/infoconex/RecordSift/releases/tag/v0.1.0) · [consumer guide](docs/release/0.1-consumer-guide.md). The GitHub tag points to the original published source commit `2db5c86bc26010afcb83c2e455d2471d8c9340d6`. [Release governance #199](https://github.com/infoconex/RecordSift/issues/199) records package checksum and successful public-feed consumer verification. Later documentation changes do not alter the immutable 0.1.0 binaries.

## Public API quick start (.NET Framework 4.8)

Install the published NuGet package into your application first; NuGet supplies the five RecordSift assemblies and their declared runtime dependencies. The following are **short illustrative fragments**, not a standalone C# 7.3 file: see the [complete runnable Console App](docs/usage/getting-started.md) for full class, namespace and entry-point declarations. Define a record type with public writable properties:

```csharp
public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### CSV import

The following example uses the public schema, property mapping, and CSV adapter. The caller chooses the character limit, header policy, source identifier, and conversion culture; no internal initializer is required.

```csharp
using System;
using System.Globalization;
using System.IO;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;

var importSchema = new ImportSchema(new[] {
    new ExpectedColumn("Id", true, true),
    new ExpectedColumn("Name", true, true)
});
var propertyMappingSchema = new PropertyMappingSchema(importSchema, new[] {
    new SourcePropertyMapping("Id", "Id"),
    new SourcePropertyMapping("Name", "Name")
});
var primitivePropertyMapper = new PrimitivePropertyMapper<Customer>(propertyMappingSchema, CultureInfo.InvariantCulture);
var csvImporter = new CsvImporter<Customer>(new CsvRecordReaderAdapter(), primitivePropertyMapper.Map);

using (var stringReader = new StringReader("Id,Name\n1,Alice\ninvalid,Bob"))
{
    var typedImportResult = csvImporter.Import(stringReader, 4096, importSchema,
        CsvHeaderPolicy.WithHeader, "customers", options: CsvReadOptions.Default);

    // Access typedImportResult.Status, typedImportResult.Summary, typedImportResult.AcceptedRecords,
    // typedImportResult.Diagnostics (including Code, Scope, Row, Field and PhysicalLine).
    Console.WriteLine(typedImportResult.Status);
    Console.WriteLine(typedImportResult.Summary.AcceptedCount);
    foreach (var importDiagnostic in typedImportResult.Diagnostics)
        Console.WriteLine(importDiagnostic.Code);
}
```

`CsvReadOptions.Default` uses a comma delimiter; supply `new CsvReadOptions(";")` for another supported delimiter. For encoded byte streams, use the public `CsvStreamImporterAdapter<T>` with an explicit `Encoding`. A headerless, nonempty CSV requires explicit `SourceColumnPosition` bindings. Passing positions with `WithHeader` is rejected, and invalid delimiters return an explicit source diagnostic.

### XLSX import

Reference `RecordSift.Infrastructure.Xlsx` and its runtime dependencies as well. Reuse the `Customer`, `importSchema`, and `primitivePropertyMapper` declarations above:

```csharp
using System.IO;
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Infrastructure.Xlsx;

var xlsxImporter = new XlsxImporter<Customer>(new XlsxWorksheetReaderAdapter(), primitivePropertyMapper.Map);
using (var fileStream = File.OpenRead("customers.xlsxImporter"))
{
    var typedImportResult = xlsxImporter.Import(fileStream, 4 * 1024 * 1024, importSchema,
        "customers", limits: XlsxReadLimits.Default);
    // The first worksheet is used by default; specify worksheetName to select one.
    // The typedImportResult exposes the same typed records, summary and diagnostic contract.
}
```

Both importers use a caller-supplied resource budget. XLSX additionally supports explicit worksheet row, column, and cell-character limits. Stream ownership remains with the caller. A failed source import has no partially accepted records; completed imports may contain record-level rejections.

The [public API acceptance tests](tests/RecordSift.Infrastructure.Csv.Tests/PublicCsvApiAcceptanceTests.cs) and [XLSX acceptance tests](tests/RecordSift.Infrastructure.Xlsx.Tests/XlsxImporterAcceptanceTests.cs) compile and execute the same public workflow on the .NET Framework 4.8 Windows CI build. These examples describe the current API, not a published binary/package compatibility promise.

## Results and JSON reports

CSV and XLSX importers return a **typed `TypedImportResult<T>`**, not a JSON string. Read `typedImportResult.AcceptedRecords` for accepted `T` objects and `typedImportResult.Status`, `typedImportResult.Summary` and `typedImportResult.Diagnostics` for outcome and validation errors.

To produce an optional JSON **outcome/diagnostic report**, reference `RecordSift.Infrastructure.Reporting` and use:

```csharp
var jsonImportResultSerializerAdapter =
    new RecordSift.Infrastructure.Reporting.JsonImportResultSerializerAdapter();
var jsonReport = jsonImportResultSerializerAdapter.Serialize(typedImportResult);
```

This report contains the source ID, status, counts and diagnostic codes/messages, **not the accepted record values**. RecordSift deliberately does not automatically export imported customer data in diagnostic JSON. To export accepted rows to JSON, serialize `typedImportResult.AcceptedRecords` in your own application using your preferred JSON library and appropriate privacy controls. See the [getting-started guide's expected console output and formatted JSON example](docs/usage/getting-started.md#2-run-a-complete-csv-import) and the [JSON contract](docs/specifications/import-result.md).

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
