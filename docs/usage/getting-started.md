# Get started with RecordSift 0.1.0

RecordSift imports **CSV and XLSX** data into your own C# classes, validates rows, and returns accepted records plus structured diagnostics. It is a **library**, not a standalone command-line converter or graphical application: you call it from your own .NET application.

**Supported for 0.1.0:** Windows, .NET Framework 4.8, C# 7.3. Microsoft Excel is **not** required. RecordSift runs locally; network access is required only to restore NuGet packages. Other .NET runtimes, Linux and macOS have not been accepted for this release.

**Published package:** [RecordSift 0.1.0 on NuGet.org](https://www.nuget.org/packages/RecordSift/0.1.0) · [GitHub v0.1.0 release](https://github.com/infoconex/RecordSift/releases/tag/v0.1.0) · [release notes](../release/0.1-release-notes.md)

## Why use RecordSift?

Use RecordSift when your application must **import, validate, and explain structured business records**, not merely read rows or worksheet cells. A single import brings together:

- CSV or XLSX source reading behind format-specific adapters, with a common typed result model.
- Declared source-column schema and explicit mapping to writable C# properties, with culture-aware primitive conversion.
- Required-value, length, range, duplicate, pattern and custom-record checks, so valid records can be accepted while invalid rows are explained.
- Ordered diagnostics with codes, record/field locations and source identifiers, plus optional JSON, readable-text and machine-interchange CSV **diagnostic reports**.
- Caller-configurable input/resource limits and a deterministic, in-process workflow; no Excel installation or runtime service dependency.

For example, a Windows line-of-business application could import a supplier's CSV today and an Excel workbook tomorrow, applying the same downstream validation and reporting conventions before saving accepted rows. **RecordSift does not persist them to a database**; your application decides what to do with the accepted records.

**Choose a simpler parser instead** if you only need to read cells or CSV rows with little mapping/validation/reporting. RecordSift builds on CsvHelper and ExcelDataReader; it does not replace their low-level parsing capabilities or promise to be faster. Release 0.1.0 also is **not a good fit** for modern .NET-only or cross-platform applications, very large streams requiring incremental processing, or workloads demanding strict process memory/CPU ceilings: this release is Windows/net48, synchronous and potentially eager.

## 1. Install the library in your own application

1. In **Visual Studio on Windows**, create a **Console App (.NET Framework)** project, selecting **.NET Framework 4.8**. This is not the similarly named modern **Console App** (.NET 8/9/etc.).
2. Right-click the project → **Manage NuGet Packages** → **Browse** → search for **RecordSift** → choose **0.1.0** → **Install**.
3. Alternatively, in Visual Studio **Tools → NuGet Package Manager → Package Manager Console**, with your application selected as the Default project, run:

   ```powershell
   Install-Package RecordSift -Version 0.1.0
   ```

4. NuGet also restores the declared dependencies, including CsvHelper 33.1.0 and ExcelDataReader 3.9.0. You should not manually copy only the RecordSift DLLs.

RecordSift provides five DLLs in one package: `RecordSift.Domain`, `RecordSift.Application`, `RecordSift.Infrastructure.Csv`, `RecordSift.Infrastructure.Xlsx` and `RecordSift.Infrastructure.Reporting`.

> **Important:** `nuget install RecordSift` from a terminal downloads packages to a folder but does **not** add a package reference to your Visual Studio project. Install the package into your **project**, as above.

## Sample files

Download the valid [CSV](../samples/customers.csv) and [Excel](../samples/customers.xlsx) datasets (20 customer rows each), or explore deliberately invalid [CSV](../samples/customers-invalid.csv) and [Excel](../samples/customers-invalid.xlsx) inputs (10 rows each). The CSV and Excel files have equivalent data, and section 2 displays all 20 valid CSV records directly. Place `customers.csv` in your Console App's working directory before running the program.

## 2. A shared customer schema for CSV and Excel

| Column | CLR type | Column required? | Value required? | Default when optional column is absent | Purpose |
| --- | --- | --- | --- | --- | --- |
| Id | `int` | Yes | Yes | — | Required integer conversion |
| Name | `string` | Yes | Yes | — | Required text |
| Email | `string` | Yes | Yes | — | Email field; syntax requires a separately configured validation rule |
| RegistrationDate | `DateTime` | Yes | Yes | — | Required date conversion |
| LoyaltyPoints | `int` | No | No | `0` | Optional integer default |
| CreditLimit | `decimal` | No | No | `0.00` | Decimal parsing |
| IsActive | `bool` | No | No | `true` | Boolean conversion |
| CustomerTier | `string` | No | No | `Standard` | Optional text default |
| LastPurchaseDate | `DateTime?` | No | No | None (`null` in model) | Nullable date |
| ReferralCode | `string` | No | No | None (`null` in model) | Optional text |

**Important:** A column that is *absent from the header* differs from a column that is present but has a *blank cell*. `ExpectedColumn.defaultValue` supplies a raw string only for an absent **optional field**; it is not a general substitution for empty cells. The 20-row downloadable dataset intentionally includes blank cells in optional columns. Do not expect `CustomerTier` or other defaults to be applied to those blank cells. The typed mapper interprets raw defaults according to destination property types and the supplied culture.

### CSV (`customers.csv`) — all 20 rows

The block below is the complete content of the downloadable CSV, not an abbreviated illustration:

```csv
Id,Name,Email,RegistrationDate,LoyaltyPoints,CreditLimit,IsActive,CustomerTier,LastPurchaseDate,ReferralCode
1001,John Smith,john.smith@example.com,2025-01-15,150,5000.00,true,Gold,2026-08-10,REF100
1002,Jane Doe,jane.doe@example.com,2025-02-20,0,2500.50,true,Standard,2026-07-22,
1003,Robert Johnson,robert.j@example.com,2025-03-10,500,10000.00,true,Platinum,2026-09-01,REF200
1004,Emily Davis,emily.d@example.com,2025-04-05,25,0.00,false,Standard,,
1005,Michael Brown,michael.b@example.com,2025-05-12,100,1500.75,true,Silver,2026-06-15,
1006,Sarah Wilson,sarah.w@example.com,2025-06-18,0,0.00,true,Standard,,
1007,David Miller,david.m@example.com,2025-07-01,750,25000.00,true,Platinum,2026-10-01,REF300
1008,Lisa Anderson,lisa.a@example.com,2025-08-25,200,7500.25,false,Gold,2026-05-20,
1009,Olivia Martin,olivia.m@example.com,2025-09-02,90,125.99,true,Silver,,FRIEND09
1010,Noah Thompson,noah.t@example.com,2025-09-17,0,0.00,false,Standard,,
1011,Ava White,ava.w@example.com,2025-10-08,320,8400.10,true,Gold,2026-09-21,AVAW
1012,Liam Harris,liam.h@example.com,2025-10-29,14,100.00,true,Standard,2026-01-03,
1013,Sophia Clark,sophia.c@example.com,2025-11-11,0,20.05,true,Standard,,
1014,Mason Lewis,mason.l@example.com,2025-12-05,135,3000.00,false,Silver,2026-02-14,MASON
1015,Isabella Young,isabella.y@example.com,2026-01-07,80,450.60,true,Gold,2026-08-12,
1016,Ethan Hall,ethan.h@example.com,2026-02-12,990,50000.00,true,Platinum,2026-10-15,VIP990
1017,Mia Allen,mia.a@example.com,2026-03-23,5,75.00,true,Standard,,
1018,Lucas King,lucas.k@example.com,2026-04-30,240,12500.50,false,Gold,2026-05-01,
1019,Amelia Wright,amelia.w@example.com,2026-05-15,30,1999.95,true,Silver,2026-06-18,WRIGHT
1020,Benjamin Scott,ben.s@example.com,2026-06-28,0,0.00,true,Standard,,
```

The Excel workbook contains the same headers and values. Different customer tiers, zero/nonzero numbers, `true`/`false` values, nullable dates and empty referral codes demonstrate normal input variations. These files are intentionally valid for the basic import. No email syntax or business-range validation is implied solely by the `ExpectedColumn` declarations.

## 3. Configure input budgets and run a complete CSV import

In 0.1.0, the CSV `maxCharacters` argument is **required** and has no built-in default. It bounds decoded UTF-16 characters processed by the reader, not row count; choose a conservative positive value based on the expected input. XLSX has a separate required compressed-byte budget. The limits below are tutorial choices, not universal safe defaults. No `App.config` is required.

Create a **Windows .NET Framework 4.8 Console App** with C# 7.3; reference `RecordSift.Domain`, `RecordSift.Application`, `RecordSift.Infrastructure.Csv`, and their runtime dependencies. Replace `Program.cs` with the following self-contained example (C# 7.3 compatible).

```csharp
using System;
using System.Globalization;
using System.IO;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;

namespace RecordSift.GettingStarted
{
    public sealed class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int LoyaltyPoints { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public string CustomerTier { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public string ReferralCode { get; set; }
    }

    internal static class Program
    {
        private static void Main()
        {
            // Location of the downloaded sample file (relative to the executable's working directory).
            var csvFilePath = "customers.csv";

            // Maximum total decoded UTF-16 characters read from the CSV input; choose a budget for the entire source, not per field.
            var maximumCsvCharacters = 4096;

            // Non-sensitive logical identifier included in source-level diagnostics.
            var sourceName = "customers";

            // The first row contains source-column names rather than customer data.
            var csvHeaderPolicy = CsvHeaderPolicy.WithHeader;

            // Use RecordSift's default CSV delimiter and parsing settings.
            var csvReadOptions = CsvReadOptions.Default;

            // Make date, decimal and numeric conversion culture-independent.
            var conversionCulture = CultureInfo.InvariantCulture;

            // Defines source columns, required values, and defaults for absent optional fields.
            var importSchema = new ImportSchema(new[]
            {
                new ExpectedColumn(name: "Id", isRequired: true, isValueRequired: true),
                new ExpectedColumn(name: "Name", isRequired: true, isValueRequired: true),
                new ExpectedColumn(name: "Email", isRequired: true, isValueRequired: true),
                new ExpectedColumn(name: "RegistrationDate", isRequired: true, isValueRequired: true),
                new ExpectedColumn(name: "LoyaltyPoints", isRequired: false, isValueRequired: false, defaultValue: "0"),
                new ExpectedColumn(name: "CreditLimit", isRequired: false, isValueRequired: false, defaultValue: "0.00"),
                new ExpectedColumn(name: "IsActive", isRequired: false, isValueRequired: false, defaultValue: "true"),
                new ExpectedColumn(name: "CustomerTier", isRequired: false, isValueRequired: false, defaultValue: "Standard"),
                new ExpectedColumn(name: "LastPurchaseDate", isRequired: false, isValueRequired: false),
                new ExpectedColumn(name: "ReferralCode", isRequired: false, isValueRequired: false)
            });

            // Maps each source column to the same-named property on Customer.
            // Explicit mappings are required by the current public API.
            var propertyMappingSchema = new PropertyMappingSchema(importSchema, new[]
            {
                new SourcePropertyMapping(sourceColumnName: "Id", propertyName: "Id"),
                new SourcePropertyMapping(sourceColumnName: "Name", propertyName: "Name"),
                new SourcePropertyMapping(sourceColumnName: "Email", propertyName: "Email"),
                new SourcePropertyMapping(sourceColumnName: "RegistrationDate", propertyName: "RegistrationDate"),
                new SourcePropertyMapping(sourceColumnName: "LoyaltyPoints", propertyName: "LoyaltyPoints"),
                new SourcePropertyMapping(sourceColumnName: "CreditLimit", propertyName: "CreditLimit"),
                new SourcePropertyMapping(sourceColumnName: "IsActive", propertyName: "IsActive"),
                new SourcePropertyMapping(sourceColumnName: "CustomerTier", propertyName: "CustomerTier"),
                new SourcePropertyMapping(sourceColumnName: "LastPurchaseDate", propertyName: "LastPurchaseDate"),
                new SourcePropertyMapping(sourceColumnName: "ReferralCode", propertyName: "ReferralCode")
            });

            // Converts raw source values into writable Customer properties.
            var primitivePropertyMapper = new PrimitivePropertyMapper<Customer>(
                propertyMappingSchema, conversionCulture);

            // Connects RecordSift's CSV reader to the typed mapping delegate.
            var csvImporter = new CsvImporter<Customer>(
                new CsvRecordReaderAdapter(), primitivePropertyMapper.Map);

            // Owns and disposes the input reader; the importer does not own the stream.
            using (var streamReader = File.OpenText(csvFilePath))
            {
                // Imports data and returns accepted records and structured diagnostics.
                var typedImportResult = csvImporter.Import(
                    streamReader,
                    maximumCsvCharacters,
                    importSchema,
                    csvHeaderPolicy,
                    sourceName,
                    options: csvReadOptions);

                // Prints the outcome without hiding unsuccessful source imports.
                Console.WriteLine("Status: " + typedImportResult.Status);

                // Prints the number of successfully mapped customer records.
                Console.WriteLine("Accepted: " + typedImportResult.Summary.AcceptedCount);

                // Process accepted customers in your application.
                // ProcessCustomers(typedImportResult.AcceptedRecords);

                // Log full diagnostic details or emit a JSON report as needed.
                // ReportDiagnostics(typedImportResult.Diagnostics);

                // Record additional summary metrics if your application needs them.
                // LogImportSummary(typedImportResult.Summary);
            }
        }
    }
}
```

The 20-row valid file should result in 20 accepted customers and no record-conversion failures under the normal invariant-culture pipeline. The example intentionally leaves business-level validation and lengthy logging helpers as comments; it still performs the real import. The source file must be in the process working directory or the path must be adjusted.

### Why explicit mapping appears repetitive

RecordSift 0.1 requires `SourcePropertyMapping(sourceColumnName: ..., propertyName: ...)` even when names match. We retain that existing API and use named arguments to clarify the intent. A possible opt-in same-name convenience API is tracked separately in [future issue #254](https://github.com/infoconex/RecordSift/issues/254); it is **not** part of this walkthrough or release.


### Optional JSON and readable reports

RecordSift returns a typed result, **not JSON**. Its JSON formatter reports the import outcome and diagnostic metadata; it does not serialize accepted `Customer` data. Reference `RecordSift.Infrastructure.Reporting` and, inside the import block from section 3, use:

```csharp
// Converts the existing result into a machine-readable diagnostic JSON report.
var jsonImportResultSerializerAdapter = new JsonImportResultSerializerAdapter();

// Contains status, counts and diagnostic details, not the accepted Customer objects.
var jsonReport = jsonImportResultSerializerAdapter.Serialize(typedImportResult);

// Converts the same result to a display-oriented text summary.
var readableImportResultFormatterAdapter = new ReadableImportResultFormatterAdapter();

// The readable summary is intended for people, not for machine parsing.
var readableReport = readableImportResultFormatterAdapter.Format(typedImportResult);

// Your application decides whether to log, display or store these reports.
// LogReports(jsonReport, readableReport);
```

Add `using RecordSift.Infrastructure.Reporting;` to the complete program to use these optional snippets. To export accepted customer data as JSON, serialize `typedImportResult.AcceptedRecords` with your application's chosen serializer and privacy controls; these reporting adapters intentionally omit record values. See the [versioned diagnostic JSON contract](../specifications/import-result.md) for schema guarantees.

### What happens to bad rows?

A `Completed` status means processing completed, not necessarily that every record was accepted. Inspect `typedImportResult.Summary.RejectedCount` and `typedImportResult.Diagnostics` separately. A `Failed` status represents a source- or schema-level failure; `Stopped` and `Canceled` have their own partial-processing semantics.

## 4. Use a real CSV file, another delimiter, or no headers

The complete program in section 3 already opens the downloaded `customers.csv` using `File.OpenText(csvFilePath)`. Its `csvFilePath`, `maximumCsvCharacters`, `importSchema`, `csvImporter`, `csvHeaderPolicy`, `sourceName`, and `csvReadOptions` are locally defined above the import call. Change the path if the sample is not in the process working directory.

For a semicolon-delimited file, set `csvReadOptions = new CsvReadOptions(";")` before import. A headerless file needs `CsvHeaderPolicy.WithoutHeader` and explicit zero-based `SourceColumnPosition` bindings (one for each supplied source column). For example:

```csharp
// Positions identify physical columns when a source has no header row.
var sourceColumnPositions = new[]
{
    new SourceColumnPosition("Id", 0),
    new SourceColumnPosition("Name", 1)
};

// Select positional rather than header-based binding.
var csvHeaderPolicy = CsvHeaderPolicy.WithoutHeader;

// Pass positions: sourceColumnPositions to csvImporter.Import(...).
```

This illustration assumes a **different two-column source**, so use a matching import schema for it; do not apply the 10-column sample schema blindly. Never combine positional bindings with `WithHeader`. For explicitly encoded streams, use the public `CsvStreamImporterAdapter<T>` with an explicit `Encoding`.

## 5. Import the same schema from Excel

Reference `RecordSift.Infrastructure.Xlsx` and its dependencies. Reuse `Customer`, `importSchema`, `propertyMappingSchema`, `conversionCulture`, and `primitivePropertyMapper` from above. Replace the CSV importer and CSV reader with:

```csharp
// Path to the Excel equivalent of the twenty-row CSV example.
var xlsxFilePath = "customers.xlsx";

// Maximum compressed workbook size in bytes; budget is caller-controlled.
var maximumWorkbookBytes = 4 * 1024 * 1024;

// Non-sensitive logical dataset name for diagnostics.
var sourceName = "customers";

// Default worksheet row, column, and cell-character safety limits.
var xlsxReadLimits = XlsxReadLimits.Default;

// Creates an XLSX importer using the same Customer mapping as CSV.
var xlsxImporter = new XlsxImporter<Customer>(
    new XlsxWorksheetReaderAdapter(), primitivePropertyMapper.Map);

// Opens and disposes the workbook input stream.
using (var fileStream = File.OpenRead(xlsxFilePath))
{
    // The first worksheet is the sample's Customers worksheet.
    var typedImportResult = xlsxImporter.Import(
        fileStream,
        maximumWorkbookBytes,
        importSchema,
        sourceName,
        limits: xlsxReadLimits);

    // Show the completion status and accepted record count.
    Console.WriteLine(typedImportResult.Status);
    Console.WriteLine(typedImportResult.Summary.AcceptedCount);

    // ProcessCustomers(typedImportResult.AcceptedRecords);
    // ReportDiagnostics(typedImportResult.Diagnostics);
}
```

The Excel snippet is an **alternative** import path within the same program, not a second standalone `Main`; include the namespaces `RecordSift.Application.Importing.Xlsx`, `RecordSift.Application.Ports.Xlsx`, and `RecordSift.Infrastructure.Xlsx` when using it. For Excel, byte and worksheet read limits are distinct from the CSV decoded-character limit.

## 6. Add record validation and create reports

For a custom record rule, construct a `CsvImporter<Customer>` using the existing schema/mapper with a Domain record rule (the record named `Blocked` will be rejected):

```csharp
// RecordValidationRule<T> is in RecordSift.Domain.Schema,
// which is already imported by the complete example above.
// In Main, instead of the earlier CsvImporter constructor:
// Reject a customer when the name matches the deliberately blocked value.
var recordValidationRules = new[]
{
    new RecordValidationRule<Customer>("Name", customer => customer.Name != "Blocked")
};
// Creates a new importer with the additional record-level validation rules.
var csvImporter = new CsvImporter<Customer>(
    new CsvRecordReaderAdapter(), primitivePropertyMapper.Map, null, null, recordValidationRules);
```

The same typed import-result object can be formatted as JSON or readable text (section 3), or as machine-interchange diagnostic CSV:

```csharp
// Serializes the diagnostic result as machine-interchange CSV (not Excel-safe).
var csvImportResultSerializerAdapter = new CsvImportResultSerializerAdapter();
// Contains diagnostics and summary fields, not accepted customer rows.
var diagnosticCsvReport = csvImportResultSerializerAdapter.Serialize(typedImportResult);
```

Here `typedImportResult` refers to the completed import result in scope. It is **not** a spreadsheet-safe report. If data contains untrusted source IDs, field names or worksheet names, opening the diagnostic CSV directly in Excel may execute spreadsheet formulas. Import CSV columns as text in spreadsheet software, or transform untrusted values using a separately verified spreadsheet-safe exporter.

For predefined rules (required values, length, numeric/date range, duplicates and regex), see the [functional behavior specification](../specifications/record-import.md) and [acceptance tests](../../tests/RecordSift.Infrastructure.Csv.Tests/PublicCsvApiAcceptanceTests.cs). For machine-readable JSON guarantees, see the [versioned result contract](../specifications/import-result.md).

### Why no built-in CSV or XLSX size default?

A generous global default could silently admit files too large for a latency-sensitive or memory-constrained application; a low one could unexpectedly reject normal customer input. The **0.1.0 public API intentionally requires the caller** to specify positive `maxCharacters` and `maxBytes`. This is different from the optional `CsvReadOptions.Default` (comma delimiter) and `XlsxReadLimits.Default` (worksheet budgets). A future overload with an explicit documented limit policy could be considered as a separately reviewed API enhancement, but do not assume it exists in 0.1.0.

## 7. Common questions and pitfalls

| Symptom | What to check |
| --- | --- |
| NuGet cannot find RecordSift | Use nuget.org as a source and select exactly **0.1.0**. Confirm your project is targeting **.NET Framework 4.8**, not modern .NET. |
| Missing RecordSift or vendor DLL at runtime | Install the **NuGet package into your project**; do not manually copy a subset of DLLs. Rebuild and restore package references. |
| Header or mapping failure | Confirm the CSV/XLSX headers exactly match your declared `ExpectedColumn` names, or configure the appropriate mapping/position mode. |
| A row has errors but status is Completed | This is normal. Read `AcceptedRecords`, `Summary.RejectedCount` and `Diagnostics` independently. |
| XLSX worksheet not found | Check the exact worksheet name, or omit `worksheetName` to select the first sheet. |
| Imports of hostile or very large files | Set conservative per-source limits. Parsing is synchronous and eagerly buffers data; **no fixed upper bound on process memory or immediate cancellation** is guaranteed. |
| Want Linux/macOS, modern .NET, async or streaming | Those are **not supported** by released 0.1.0. Do not assume compatibility from the package name. |

## Further reading

- [Consumer guide: formats, dependency behavior and safety](../release/0.1-consumer-guide.md)
- [Release notes and limitations](../release/0.1-release-notes.md)
- [Structured result and versioned JSON format](../specifications/import-result.md)
- [Release 0.1 security assessment](../engineering/release-0.1-security-assessment.md)
- [Published v0.1.0 release and package provenance](https://github.com/infoconex/RecordSift/releases/tag/v0.1.0)
- [Public C# CSV API tests](../../tests/RecordSift.Infrastructure.Csv.Tests/PublicCsvApiAcceptanceTests.cs) and [XLSX tests](../../tests/RecordSift.Infrastructure.Xlsx.Tests/XlsxImporterAcceptanceTests.cs)

**Documentation version:** This guide applies to the published, immutable `RecordSift 0.1.0` API. Edits to this guide after publication do not change the bytes or capabilities of the already-published package.
