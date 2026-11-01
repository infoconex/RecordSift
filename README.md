# RecordSift

**Import, validate, and report on structured data.**

RecordSift is an MIT-licensed .NET Framework 4.8 library under development for importing CSV and XLSX records, applying domain-owned validation, and returning structured diagnostics and reports.

## Project status

**Release 0.1 has passed unpublished release-candidate and stable 0.1.0 package preflight; no public NuGet package or GitHub release has been published.** The repository owner approved MIT licensing with Copyright (c) 2026 Infoconex; see [LICENSE](LICENSE). This license permits redistribution subject to the MIT terms, but no 0.1 package has been published or authorized for release. The current source builds as separate Domain, Application, and vendor-isolated Infrastructure assemblies. CSV and XLSX import, named worksheet selection, typed mapping, validation, structured results, JSON serialization, and readable summaries are implemented and covered by automated tests. All targeted functional and Windows-scoped nonfunctional acceptance is complete; packaging and clean-consumer verification passed for the internal prerelease and stable package identities. Promotion into `main` and package publication are separately controlled release operations; public distribution still requires explicit authorization.

## Public API quick start (.NET Framework 4.8)

For source development, reference the built `RecordSift.Domain`, `RecordSift.Application`, and `RecordSift.Infrastructure.Csv` assemblies (including the infrastructure adapter's runtime dependencies). An unpublished five-assembly NuGet candidate is available for local internal verification only, not public redistribution. Define a record type with public writable properties:

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

var schema = new ImportSchema(new[] {
    new ExpectedColumn("Id", true, true),
    new ExpectedColumn("Name", true, true)
});
var properties = new PropertyMappingSchema(schema, new[] {
    new SourcePropertyMapping("Id", "Id"),
    new SourcePropertyMapping("Name", "Name")
});
var mapper = new PrimitivePropertyMapper<Customer>(properties, CultureInfo.InvariantCulture);
var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(), mapper.Map);

using (var csv = new StringReader("Id,Name\n1,Alice\ninvalid,Bob"))
{
    var result = importer.Import(csv, 4096, schema,
        CsvHeaderPolicy.WithHeader, "customers", options: CsvReadOptions.Default);

    // Access result.Status, result.Summary, result.AcceptedRecords,
    // result.Diagnostics (including Code, Scope, Row, Field and PhysicalLine).
    Console.WriteLine(result.Status);
    Console.WriteLine(result.Summary.AcceptedCount);
    foreach (var error in result.Diagnostics)
        Console.WriteLine(error.Code);
}
```

`CsvReadOptions.Default` uses a comma delimiter; supply `new CsvReadOptions(";")` for another supported delimiter. For encoded byte streams, use the public `CsvStreamImporterAdapter<T>` with an explicit `Encoding`. A headerless, nonempty CSV requires explicit `SourceColumnPosition` bindings. Passing positions with `WithHeader` is rejected, and invalid delimiters return an explicit source diagnostic.

### XLSX import

Reference `RecordSift.Infrastructure.Xlsx` and its runtime dependencies as well. Reuse the `Customer`, `schema`, and `mapper` declarations above:

```csharp
using System.IO;
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Infrastructure.Xlsx;

var xlsx = new XlsxImporter<Customer>(new XlsxWorksheetReaderAdapter(), mapper.Map);
using (var stream = File.OpenRead("customers.xlsx"))
{
    var result = xlsx.Import(stream, 4 * 1024 * 1024, schema,
        "customers", limits: XlsxReadLimits.Default);
    // The first worksheet is used by default; specify worksheetName to select one.
    // The result exposes the same typed records, summary and diagnostic contract.
}
```

Both importers use a caller-supplied resource budget. XLSX additionally supports explicit worksheet row, column, and cell-character limits. Stream ownership remains with the caller. A failed source import has no partially accepted records; completed imports may contain record-level rejections.

The [public API acceptance tests](tests/RecordSift.Infrastructure.Csv.Tests/PublicCsvApiAcceptanceTests.cs) and [XLSX acceptance tests](tests/RecordSift.Infrastructure.Xlsx.Tests/XlsxImporterAcceptanceTests.cs) compile and execute the same public workflow on the .NET Framework 4.8 Windows CI build. These examples describe the current API, not a published binary/package compatibility promise.

## XLSX input security boundary

The XLSX reader independently checks ZIP entry count (512 maximum), both declared and actually decompressed package bytes (the smaller of 64 MiB or 32 times the caller's compressed-byte budget), and shared-string capacity hints and item counts (one million maximum). XML inspected during shared-string preflight prohibits DTDs and external entity resolution. These checks run before ExcelDataReader 3.9.0 parses the workbook, in addition to caller-controlled row/column/cell limits. The read is synchronous and eagerly buffers data; these safeguards do **not** guarantee a fixed maximum process heap, interrupt an expensive parse immediately on cancellation, or establish that all malformed files are harmless. Set conservative input budgets for untrusted XLSX. See the [release security assessment](docs/engineering/release-0.1-security-assessment.md).

## Diagnostic CSV security boundary

`CsvImportResultSerializerAdapter` produces deterministic **machine-interchange CSV**, not a spreadsheet-safe export. Caller-controlled source, field and worksheet identifiers are preserved as original text. CSV quoting handles delimiters and line breaks but does not prevent applications such as Excel from treating values beginning with `=`, `+`, `-` or `@` as formulas. Do **not** directly open diagnostic exports containing untrusted metadata in spreadsheet software; import columns explicitly as text or apply a separately tested spreadsheet-specific transformation. This library does not silently alter metadata to provide spreadsheet formula protection. See the [FR-038 report contract](docs/scenarios/functional/FR-038.md) and [consumer safety guidance](docs/release/0.1-consumer-guide.md).

## Development baseline

- [Release-candidate readiness assessment](docs/engineering/release-0.1-readiness.md)
- [Candidate consumer guide](docs/release/0.1-consumer-guide.md)
- [Candidate release notes and known limitations](docs/release/0.1-release-notes.md)
- [Release verification and promotion checklist](docs/release/0.1-release-checklist.md)
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
