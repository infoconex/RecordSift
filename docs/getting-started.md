# Getting started: import customer records from CSV or Excel

RecordSift 0.1 targets Windows, .NET Framework 4.8, and C# 7.3. The same customer model and declared schema work for both formats. The walkthrough uses the existing public API; it does not require application configuration files or new mapping APIs.

## 1. Download the sample files

- [customers.csv](samples/customers.csv) — 20 valid rows, used in the CSV walkthrough.
- [customers.xlsx](samples/customers.xlsx) — the same 20 rows in Excel format (first worksheet: `Customers`).
- [customers-invalid.csv](samples/customers-invalid.csv) — 10 deliberately problematic rows for diagnostics practice.
- [customers-invalid.xlsx](samples/customers-invalid.xlsx) — the same deliberately problematic rows in Excel format.

The examples contain fictional customers only. Add `customers.csv` to the console project's output directory (or update the local path). For Excel, copy `customers.xlsx` alongside it. Install/reference the RecordSift package or assemblies and their dependencies as described in the [consumer guide](release/0.1-consumer-guide.md).

## 2. Shared customer schema

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

            // Upper bound on decoded characters in one CSV field; choose a limit for your data.
            var maximumFieldLength = 4096;

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
                    maximumFieldLength,
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

## 4. Import the same schema from Excel

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

## 5. Exercise diagnostics intentionally

After a successful import, switch the path to `customers-invalid.csv` (or the matching XLSX sample). Those samples include blank required values and deliberately invalid integer, decimal, Boolean and date literals. These should demonstrate conversion or required-value diagnostics without altering the normal successful walkthrough.

An invalid **email syntax** value and negative loyalty points are also included. They will not automatically be rejected merely because the property is named `Email` or `LoyaltyPoints`; configure RecordSift's explicit record-validation rules to enforce those additional business constraints. Import results expose `Status`, `Summary`, `AcceptedRecords`, and `Diagnostics`. Use diagnostic `Code`, `Scope`, `Row`, and `Field` for machine-readable troubleshooting. Source failures are different from per-record rejections.

For format-neutral report serialization and safety considerations, see the [consumer guide](release/0.1-consumer-guide.md). Diagnostic CSV reports are machine interchange files, **not** spreadsheet-formula-safe exports.
