# Get started with RecordSift 0.1.0

RecordSift imports **CSV and XLSX** data into your own C# classes, validates rows, and returns accepted records plus structured diagnostics. It is a **library**, not a standalone command-line converter or graphical application: you call it from your own .NET application.

**Supported for 0.1.0:** Windows, .NET Framework 4.8, C# 7.3. Microsoft Excel is **not** required. RecordSift runs locally; network access is required only to restore NuGet packages. Other .NET runtimes, Linux and macOS have not been accepted for this release.

**Published package:** [RecordSift 0.1.0 on NuGet.org](https://www.nuget.org/packages/RecordSift/0.1.0) · [GitHub v0.1.0 release](https://github.com/infoconex/RecordSift/releases/tag/v0.1.0) · [release notes](../release/0.1-release-notes.md)

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

## 2. Run a complete CSV import

Replace the content of `Program.cs` in your new .NET Framework 4.8 Console App with this **complete C# 7.3 program** (no top-level statements):

```csharp
using System;
using System.Globalization;
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
}

internal static class Program
{
    private static void Main()
    {
        // 1. Declare which source columns are expected.
        var schema = new ImportSchema(new[]
        {
            new ExpectedColumn("Id", true, true),
            new ExpectedColumn("Name", true, true)
        });

        // 2. Map incoming column names to writable Customer properties.
        var mapping = new PropertyMappingSchema(schema, new[]
        {
            new SourcePropertyMapping("Id", "Id"),
            new SourcePropertyMapping("Name", "Name")
        });
        var mapper = new PrimitivePropertyMapper<Customer>(
            mapping, CultureInfo.InvariantCulture);

        // 3. Create an importer with the real CSV adapter.
        var importer = new CsvImporter<Customer>(
            new CsvRecordReaderAdapter(), mapper.Map);

        // 4. Import. The second row contains an intentionally invalid Id.
        using (var source = new StringReader("Id,Name\n1,Alice\nnot-an-integer,Bob"))
        {
            var result = importer.Import(
                source, 4096, schema, CsvHeaderPolicy.WithHeader,
                "demo-customers", options: CsvReadOptions.Default);

            Console.WriteLine("Status: " + result.Status);
            Console.WriteLine(
                "Processed: " + result.Summary.ProcessedCount +
                ", accepted: " + result.Summary.AcceptedCount +
                ", rejected: " + result.Summary.RejectedCount);

            foreach (var customer in result.AcceptedRecords)
                Console.WriteLine(customer.Id + ": " + customer.Name);

            foreach (var diagnostic in result.Diagnostics)
                Console.WriteLine(
                    "Error " + diagnostic.Code +
                    " at data row " + diagnostic.Row +
                    ", field " + diagnostic.Field);

            // 5. Reports are generated from the result, not by re-reading CSV.
            var json = new JsonImportResultSerializerAdapter().Serialize(result);
            var text = new ReadableImportResultFormatterAdapter().Format(result);
            Console.WriteLine(json);
            Console.WriteLine(text);
        }
    }
}
```

Build and run the project. For this specific input, expect:

```text
Status: Completed
Processed: 2, accepted: 1, rejected: 1
1: Alice
Error CONVERSION_FAILED at data row 2, field Id
```

The program then prints a JSON report and a readable summary. **The exact report text may be longer**; use the structured `Status`, `Summary`, and `Diagnostics` members for application logic rather than parsing human-readable messages. The example uses `StringReader` so you can run it immediately. To read a real CSV file, replace the `StringReader` with `File.OpenText("customers.csv")` (or use the explicitly encoded byte-stream adapter described in the [consumer guide](../release/0.1-consumer-guide.md)).

### What happens to bad rows?

`Status == ImportStatus.Completed` means the source processing completed. It does **not** mean every row was valid. In the example, the valid Alice row is accepted; Bob is rejected because his `Id` cannot be converted to an `int`. One rejected row may have multiple diagnostics. Inspect `result.Summary` for row counts and `result.Diagnostics` for detailed machine-readable problems.

`ImportStatus.Failed` represents a source- or schema-level failure, not merely a bad data row. `Stopped` indicates a deliberate fail-fast stop; `Canceled` indicates cooperative cancellation. Do not treat these statuses as equivalent or assume they all contain the same accepted-record information.

## 3. Use a real CSV file, a different delimiter, or no headers

For a regular UTF-8 CSV file with a header row:

```csharp
using (var reader = File.OpenText(@"C:\data\customers.csv"))
{
    var result = importer.Import(reader, 1000000, schema,
        CsvHeaderPolicy.WithHeader, "customer-upload",
        options: CsvReadOptions.Default);
    // Inspect result.Status, result.AcceptedRecords and result.Diagnostics.
}
```

Here `schema` and `importer` are the objects initialized in section 2; this is an **alternative** input block, not a second complete program. `1000000` is an example **decoded-character budget** for this `TextReader` API, not a recommended universal maximum.

For a semicolon-delimited file, pass `options: new CsvReadOptions(";")`. For a headerless file, select `CsvHeaderPolicy.WithoutHeader` and provide explicit zero-based positions, for example:

```csharp
positions: new[]
{
    new SourceColumnPosition("Id", 0),
    new SourceColumnPosition("Name", 1)
}
```

Add the `positions:` named argument to the `Import(...)` call and supply a headerless source; **do not** combine positions with `CsvHeaderPolicy.WithHeader`. For non-UTF-8 files or strict decoding, use the byte-stream importer with an explicitly chosen encoding instead of relying on `File.OpenText` defaults.

## 4. Import an Excel workbook (XLSX)

The same `Customer`, `schema`, and `mapper` from section 2 can be reused for an XLSX workbook with `Id` and `Name` column headers. Add these namespaces:

```csharp
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Infrastructure.Xlsx;
```

Then, in `Main`, create the XLSX importer:

```csharp
var xlsxImporter = new XlsxImporter<Customer>(
    new XlsxWorksheetReaderAdapter(), mapper.Map);

using (var workbook = File.OpenRead(@"C:\data\customers.xlsx"))
{
    var result = xlsxImporter.Import(
        workbook, 4 * 1024 * 1024, schema, "customer-workbook",
        worksheetName: "Customers", limits: XlsxReadLimits.Default);

    Console.WriteLine(result.Status);
    Console.WriteLine("Accepted: " + result.Summary.AcceptedCount);
    foreach (var error in result.Diagnostics)
        Console.WriteLine(error.Code + " / " + error.Worksheet +
                          " / row " + error.PhysicalRow);
}
```

`worksheetName: "Customers"` selects that worksheet by its **exact name**; omit it to read the first worksheet. Missing worksheets return a source-level diagnostic rather than silently selecting another sheet. The `4 * 1024 * 1024` value is the **compressed workbook byte limit**, and `XlsxReadLimits` separately controls row/column/cell-character budgets. The caller owns the stream. The library never starts Microsoft Excel or evaluates formulas/macros.

## 5. Add record validation and create reports

For a custom record rule, construct a `CsvImporter<Customer>` using the existing schema/mapper with a Domain record rule (the record named `Blocked` will be rejected):

```csharp
// RecordValidationRule<T> is in RecordSift.Domain.Schema,
// which is already imported by the complete example above.
// In Main, instead of the earlier CsvImporter constructor:
var rules = new[]
{
    new RecordValidationRule<Customer>("Name", customer => customer.Name != "Blocked")
};
var importer = new CsvImporter<Customer>(
    new CsvRecordReaderAdapter(), mapper.Map, null, null, rules);
```

The same typed import-result object can be formatted as JSON or readable text (section 2), or as machine-interchange diagnostic CSV:

```csharp
var diagnosticCsv = new CsvImportResultSerializerAdapter().Serialize(result);
```

Here `result` refers to the completed import result in scope. It is **not** a spreadsheet-safe report. If data contains untrusted source IDs, field names or worksheet names, opening the diagnostic CSV directly in Excel may execute spreadsheet formulas. Import CSV columns as text in spreadsheet software, or transform untrusted values using a separately verified spreadsheet-safe exporter.

For predefined rules (required values, length, numeric/date range, duplicates and regex), see the [functional behavior specification](../specifications/record-import.md) and [acceptance tests](../../tests/RecordSift.Infrastructure.Csv.Tests/PublicCsvApiAcceptanceTests.cs). For machine-readable JSON guarantees, see the [versioned result contract](../specifications/import-result.md).

## 6. Common questions and pitfalls

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
