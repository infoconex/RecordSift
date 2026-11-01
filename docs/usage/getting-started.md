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

## 2. A shared customer schema for CSV and Excel

Both formats use the **same six logical columns**, the same C# `Customer` type, and the same `ImportSchema` / `PropertyMappingSchema`. CSV headings and XLSX worksheet headings use the same names (case-sensitive).

| Source column | C# destination property | Type | Column required? | Value required if present? | If the whole column is omitted |
| --- | --- | --- | --- | --- | --- |
| `Id` | `Id` | `int` | Yes | Yes | Source/schema error |
| `Name` | `Name` | `string` | Yes | Yes | Source/schema error |
| `LoyaltyPoints` | `LoyaltyPoints` | `int` | No | No | `0` |
| `CreditLimit` | `CreditLimit` | `decimal` | No | No | `0.00` |
| `IsActive` | `IsActive` | `bool` | No | No | `true` |
| `Email` | `Email` | `string` | No | No | `null` (no explicit default) |

`ExpectedColumn.IsRequired` controls **whether the column exists**; `IsValueRequired` controls **whether a present row's value may be blank**. For optional value-type properties, use an explicit raw-string default (e.g. `"0"`, `"0.00"`, `"true"`) so your business default is documented and applied by the mapping pipeline. `Email` has no configured default, so a newly constructed `Customer.Email` remains `null` when that column is omitted. Without an explicit default, absent optional nonnullable value-type properties keep their CLR initialization (`0`, `false`, etc.); a property initializer could also affect the initial value.

**Defaults apply only when the entire optional source column is absent from the header**, not when an existing column has an empty cell. A blank value for a nonnullable `int`, `decimal`, or `bool` will fail primitive conversion instead of being replaced with the absence default; use a nullable value type (e.g., `int?`) when a blank is intended to represent `null`. An explicit default is parsed according to the mapper's supplied culture (`InvariantCulture` in this tutorial).

These files both satisfy the shared schema and use the same import/mapping/validation rules:

**CSV (`customers.csv`):**

```csv
Id,Name
1,Alice
not-an-integer,Bob
```

**Excel (`customers.xlsx`, worksheet `Customers`):** put `Id` and `Name` in cells A1 and B1, with `1` / `Alice` in A2:B2 and `not-an-integer` / `Bob` in A3:B3. The other four optional columns are **absent** in both inputs. You can add those four columns later, using the exact names in the table, without changing the C# schema or model. For example, the full heading row in either CSV or the XLSX worksheet is `Id,Name,LoyaltyPoints,CreditLimit,IsActive,Email`.

For Alice, RecordSift maps missing optional fields to `LoyaltyPoints = 0`, `CreditLimit = 0.00m`, `IsActive = true`, and `Email = null`. Bob is rejected because his required `Id` is invalid; the imported result can still have `Status = Completed`.

## 3. Configure input budgets and run a complete CSV import

In 0.1.0, the CSV `maxCharacters` and XLSX `maxBytes` arguments are **required**. They do not have library-provided defaults because each application must choose appropriate resource limits for its inputs and threat model. `maxCharacters` counts **decoded UTF-16 characters** read by the `TextReader`; `maxBytes` counts the **compressed XLSX input bytes**. Neither is a maximum number of rows. Both must be positive.

For a real application, keep these limits in application configuration rather than scattering unexplained integer literals through `Import(...)` calls. Add an **App.config** file to the **Console App project** (or edit its existing one) with the following sample values. They are **tutorial examples, not safe universal defaults**; choose limits from your own observed file sizes, resource constraints and acceptance policy.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <!-- Example decoded CSV character budget: 64 Ki UTF-16 code units -->
    <add key="RecordSift.MaxCsvCharacters" value="65536" />
    <!-- Example compressed XLSX file budget: 4 MiB -->
    <add key="RecordSift.MaxXlsxBytes" value="4194304" />
  </appSettings>
</configuration>
```

The code below reads the configured CSV limit by name and rejects missing, invalid or nonpositive values. Add a reference to the .NET Framework **System.Configuration** assembly if your Console App project does not already reference it. `System.Configuration` is part of the .NET Framework 4.8 environment; this is application configuration, **not** a new RecordSift dependency.

Replace the content of `Program.cs` in your Console App with this **complete C# 7.3 program** (no top-level statements). The sample imports the required columns only, so the optional defaults are exercised:

```csharp
using System;
using System.Globalization;
using System.Configuration;
using System.IO;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;
using RecordSift.Infrastructure.Reporting;

internal sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
    public string Email { get; set; }
}

internal static class Program
{
    // Read explicit application budgets rather than relying on a hidden global limit.
    private static int ReadPositiveLimit(string settingName)
    {
        var configuredValue = ConfigurationManager.AppSettings[settingName];
        int limit;
        if (!int.TryParse(configuredValue, NumberStyles.None,
                CultureInfo.InvariantCulture, out limit) || limit < 1)
            throw new ConfigurationErrorsException(
                "A positive integer App.config value is required for " + settingName);
        return limit;
    }

    private static void Main()
    {
        // 1. Declare which source columns are expected.
        var importSchema = new ImportSchema(new[]
        {
            new ExpectedColumn("Id", true, true),
            new ExpectedColumn("Name", true, true),
            new ExpectedColumn("LoyaltyPoints", false, false, "0"),
            new ExpectedColumn("CreditLimit", false, false, "0.00"),
            new ExpectedColumn("IsActive", false, false, "true"),
            new ExpectedColumn("Email", false, false)
        });

        // 2. Map incoming column names to writable Customer properties.
        var propertyMappingSchema = new PropertyMappingSchema(importSchema, new[]
        {
            new SourcePropertyMapping("Id", "Id"),
            new SourcePropertyMapping("Name", "Name"),
            new SourcePropertyMapping("LoyaltyPoints", "LoyaltyPoints"),
            new SourcePropertyMapping("CreditLimit", "CreditLimit"),
            new SourcePropertyMapping("IsActive", "IsActive"),
            new SourcePropertyMapping("Email", "Email")
        });
        var primitivePropertyMapper = new PrimitivePropertyMapper<Customer>(
            propertyMappingSchema, CultureInfo.InvariantCulture);

        // 3. Create the CSV importer with the real CSV adapter.
        var csvImporter = new CsvImporter<Customer>(
            new CsvRecordReaderAdapter(), primitivePropertyMapper.Map);

        // 4. Import. The second row contains an intentionally invalid Id.
        using (var stringReader = new StringReader("Id,Name\n1,Alice\nnot-an-integer,Bob"))
        {
            var maxCsvCharacters = ReadPositiveLimit("RecordSift.MaxCsvCharacters");
            var typedImportResult = csvImporter.Import(
                stringReader, maxCsvCharacters, importSchema, CsvHeaderPolicy.WithHeader,
                "demo-customers", options: CsvReadOptions.Default);

            Console.WriteLine("Status: " + typedImportResult.Status);
            Console.WriteLine(
                "Processed: " + typedImportResult.Summary.ProcessedCount +
                ", accepted: " + typedImportResult.Summary.AcceptedCount +
                ", rejected: " + typedImportResult.Summary.RejectedCount);

            foreach (var customer in typedImportResult.AcceptedRecords)
                Console.WriteLine(
                    customer.Id + ": " + customer.Name +
                    " (points=" + customer.LoyaltyPoints +
                    ", creditLimit=" + customer.CreditLimit.ToString("0.00", CultureInfo.InvariantCulture) +
                    ", isActive=" + customer.IsActive +
                    ", email=" + (customer.Email ?? "<none>") + ")");

            foreach (var importDiagnostic in typedImportResult.Diagnostics)
                Console.WriteLine(
                    "Error " + importDiagnostic.Code +
                    " at data row " + importDiagnostic.Row +
                    ", field " + importDiagnostic.Field);

            // 5. Reports are generated from the imported result, not by re-reading CSV.
            var jsonImportResultSerializerAdapter = new JsonImportResultSerializerAdapter();
            var jsonReport = jsonImportResultSerializerAdapter.Serialize(typedImportResult);
            var readableImportResultFormatterAdapter = new ReadableImportResultFormatterAdapter();
            var readableReport = readableImportResultFormatterAdapter.Format(typedImportResult);
            Console.WriteLine("JSON report:");
            Console.WriteLine(jsonReport);
            Console.WriteLine("Readable summary:");
            Console.WriteLine(readableReport);
        }
    }
}
```

Build and run the project. The **first four console lines** for this specific input are:

```text
Status: Completed
Processed: 2, accepted: 1, rejected: 1
1: Alice (points=0, creditLimit=0.00, isActive=True, email=<none>)
Error CONVERSION_FAILED at data row 2, field Id
```

This explicitly demonstrates that the four absent optional source columns were handled consistently. The outcome counts and JSON diagnostic report remain the same regardless of whether the input is CSV or XLSX, given equivalent source rows.

Next, the program prints `JSON report:` and a JSON string. The JSON below illustrates the **actual public 1.0 report contract** for this input, **pretty-printed for readability** (the adapter's string itself is compact):

```json
{
  "schemaVersion": "1.0",
  "sourceId": "demo-customers",
  "status": "Completed",
  "message": "Import completed with rejected records.",
  "summary": {
    "processedCount": 2,
    "acceptedCount": 1,
    "rejectedCount": 1
  },
  "diagnostics": [
    {
      "scope": "Record",
      "row": 2,
      "field": "Id",
      "code": "CONVERSION_FAILED",
      "message": "A field could not be converted.",
      "physicalLine": 3
    }
  ]
}
```

Finally, the program prints `Readable summary:` and the human-readable text report. Its text is intended for display, **not for machine parsing**. If you want to save a JSON report instead of printing it, call `File.WriteAllText("import-report.json", jsonReport);` in `Main` after creating the `jsonReport` variable (with appropriate filesystem error handling in production).

**Important: the JSON report does not contain the accepted customer records.** The original typed `typedImportResult.AcceptedRecords` contains `Customer { Id = 1, Name = "Alice" }`; this is intentional to avoid exporting source record values in default diagnostic reports. If you need a **JSON file containing imported data**, serialize `typedImportResult.AcceptedRecords` in your *own application*, under an appropriate privacy policy, using your chosen JSON serializer. RecordSift's `JsonImportResultSerializerAdapter` is for reporting **outcomes and diagnostics**, not for exporting your data records.

The JSON `schemaVersion` is independent of the NuGet package version: completed and source-failed results use **1.0**, opt-in fail-fast stopped results use **1.1**, and canceled results use **1.2**. See the [versioned JSON contract](../specifications/import-result.md) and the [tested golden fixtures](../../tests/RecordSift.Infrastructure.Reporting.Tests/Fixtures/Compatibility/) for exact field guarantees.

Use structured `Status`, `Summary` and `Diagnostics` members for application logic rather than parsing text messages. The example uses `StringReader` so you can run it immediately. For a real CSV file, replace it with `File.OpenText("customers.csv")` (or use the explicitly encoded byte-stream adapter described in the [consumer guide](../release/0.1-consumer-guide.md)).

### What happens to bad rows?

`Status == ImportStatus.Completed` means the source processing completed. It does **not** mean every row was valid. In the example, the valid Alice row is accepted; Bob is rejected because his `Id` cannot be converted to an `int`. One rejected row may have multiple diagnostics. Inspect `typedImportResult.Summary` for row counts and `typedImportResult.Diagnostics` for detailed machine-readable problems.

`ImportStatus.Failed` represents a source- or schema-level failure, not merely a bad data row. `Stopped` indicates a deliberate fail-fast stop; `Canceled` indicates cooperative cancellation. Do not treat these statuses as equivalent or assume they all contain the same accepted-record information.

## 4. Use a real CSV file, a different delimiter, or no headers

For a regular UTF-8 CSV file with a header row:

```csharp
using (var streamReader = File.OpenText(@"C:\data\customers.csv"))
{
    var maxCsvCharacters = ReadPositiveLimit("RecordSift.MaxCsvCharacters");
    var typedImportResult = csvImporter.Import(streamReader, maxCsvCharacters, importSchema,
        CsvHeaderPolicy.WithHeader, "customer-upload",
        options: CsvReadOptions.Default);
    // Inspect typedImportResult.Status, typedImportResult.AcceptedRecords and typedImportResult.Diagnostics.
}
```

Here `importSchema`, `csvImporter`, and the `ReadPositiveLimit` method are initialized in section 3; this is an **alternative** input block, not a second complete program. Its input budget comes from the same `RecordSift.MaxCsvCharacters` application setting, regardless of whether the source is a `StringReader` or a file-backed `StreamReader`.

For a semicolon-delimited file, pass `options: new CsvReadOptions(";")`. For a headerless file, select `CsvHeaderPolicy.WithoutHeader` and provide explicit zero-based positions, for example:

```csharp
positions: new[]
{
    new SourceColumnPosition("Id", 0),
    new SourceColumnPosition("Name", 1)
}
```

Add the `positions:` named argument to the `Import(...)` call and supply a headerless source; **do not** combine positions with `CsvHeaderPolicy.WithHeader`. For non-UTF-8 files or strict decoding, use the byte-stream importer with an explicitly chosen encoding instead of relying on `File.OpenText` defaults.

## 5. Import an Excel workbook (XLSX)

The **same** `Customer`, `importSchema`, and `primitivePropertyMapper` from the shared column table and CSV example can be reused for an XLSX workbook with `Id` and `Name` column headers. Omitted optional headings use the same defaults. Add these namespaces:

```csharp
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Infrastructure.Xlsx;
```

Then, in `Main`, create the XLSX importer:

```csharp
var xlsxImporter = new XlsxImporter<Customer>(
    new XlsxWorksheetReaderAdapter(), primitivePropertyMapper.Map);

using (var fileStream = File.OpenRead(@"C:\data\customers.xlsx"))
{
    var maxXlsxBytes = ReadPositiveLimit("RecordSift.MaxXlsxBytes");
    var typedImportResult = xlsxImporter.Import(
        fileStream, maxXlsxBytes, importSchema, "customer-workbook",
        worksheetName: "Customers", limits: XlsxReadLimits.Default);

    Console.WriteLine(typedImportResult.Status);
    Console.WriteLine("Accepted: " + typedImportResult.Summary.AcceptedCount);
    foreach (var importDiagnostic in typedImportResult.Diagnostics)
        Console.WriteLine(importDiagnostic.Code + " / " + importDiagnostic.Worksheet +
                          " / row " + importDiagnostic.PhysicalRow);
}
```

`worksheetName: "Customers"` selects that worksheet by its **exact name**; omit it to read the first worksheet. Missing worksheets return a source-level diagnostic rather than silently selecting another sheet. `maxXlsxBytes` is the **compressed workbook byte limit** read from the `RecordSift.MaxXlsxBytes` App.config setting. It has no library default. `XlsxReadLimits.Default` independently limits the one-based physical worksheet row index to **100,000**, columns per row to **256**, and decoded UTF-16 characters per cell to **32,767**. These worksheet defaults do **not** override the mandatory file byte limit. The caller owns the stream. The library never starts Microsoft Excel or evaluates formulas/macros.

## 6. Add record validation and create reports

For a custom record rule, construct a `CsvImporter<Customer>` using the existing schema/mapper with a Domain record rule (the record named `Blocked` will be rejected):

```csharp
// RecordValidationRule<T> is in RecordSift.Domain.Schema,
// which is already imported by the complete example above.
// In Main, instead of the earlier CsvImporter constructor:
var recordValidationRules = new[]
{
    new RecordValidationRule<Customer>("Name", customer => customer.Name != "Blocked")
};
var csvImporter = new CsvImporter<Customer>(
    new CsvRecordReaderAdapter(), primitivePropertyMapper.Map, null, null, recordValidationRules);
```

The same typed import-result object can be formatted as JSON or readable text (section 3), or as machine-interchange diagnostic CSV:

```csharp
var csvImportResultSerializerAdapter = new CsvImportResultSerializerAdapter();
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
