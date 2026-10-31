using System;
using System.Globalization;
using System.IO;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;
using RecordSift.Infrastructure.Xlsx;
using RecordSift.Infrastructure.Reporting;

// Compiled outside the repository against only NuGet-installed assemblies.
internal static class Program
{
    private sealed class Customer
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    private const string Workbook = "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";

    private static void Require(bool valid, string caseName)
    {
        if (!valid) throw new InvalidOperationException("Clean consumer: " + caseName);
    }

    public static int Main()
    {
        var schema = new ImportSchema(new[] {
            new ExpectedColumn("Name", true, true), new ExpectedColumn("Id", true, true)
        });
        var mapping = new PropertyMappingSchema(schema, new[] {
            new SourcePropertyMapping("Name", "Name"), new SourcePropertyMapping("Id", "Id")
        });
        var mapper = new PrimitivePropertyMapper<Customer>(mapping, CultureInfo.InvariantCulture);
        var rules = new[] { new RecordValidationRule<Customer>("Name", c => c.Name != "Blocked") };
        var csv = new CsvImporter<Customer>(new CsvRecordReaderAdapter(), mapper.Map, null, null, rules);
        TypedImportResult<Customer> result;
        using (var reader = new StringReader("Name,Id\nAlice,1\nBlocked,2\nBad,invalid\n"))
            result = csv.Import(reader, 4096, schema, CsvHeaderPolicy.WithHeader, "consumer-csv",
                options: CsvReadOptions.Default);

        Require(result.Status == ImportStatus.Completed, "CSV status");
        Require(result.Summary.ProcessedCount == 3, "CSV processed");
        Require(result.Summary.AcceptedCount == 1, "CSV accepted");
        Require(result.Summary.RejectedCount == 2, "CSV rejected");
        Require(result.AcceptedRecords[0].Name == "Alice", "CSV schema and mapping");
        Require(result.Diagnostics.Count == 2, "CSV conversion and validation");
        var json = new JsonImportResultSerializerAdapter().Serialize(result);
        var readable = new ReadableImportResultFormatterAdapter().Format(result);
        var report = new CsvImportResultSerializerAdapter().Serialize(result);
        Require(json.Contains("\"schemaVersion\":\"1.0\""), "JSON report");
        Require(json.Contains("consumer-csv"), "JSON source");
        Require(readable.Contains("2"), "readable summary");
        Require(report.StartsWith("SourceId,Scope,Row,Field,Code,Message"), "CSV report header");
        Require(report.Contains("consumer-csv"), "CSV diagnostic rows");

        var xlsx = new XlsxImporter<Customer>(new XlsxWorksheetReaderAdapter(), mapper.Map);
        using (var stream = new MemoryStream(Convert.FromBase64String(Workbook)))
        {
            var imported = xlsx.Import(stream, 4096, schema, "consumer-xlsx");
            Require(imported.Status == ImportStatus.Completed, "XLSX status");
            Require(imported.Summary.ProcessedCount == 3, "XLSX processed");
            Require(imported.Summary.AcceptedCount == 2, "XLSX accepted");
            Require(imported.Summary.RejectedCount == 1, "XLSX rejected");
            Require(imported.AcceptedRecords[0].Name == "Alice", "XLSX mapping A");
            Require(imported.AcceptedRecords[1].Name == "Bob", "XLSX mapping B");
            Require(imported.Diagnostics.Count == 1, "XLSX diagnostics");
            Require(new JsonImportResultSerializerAdapter().Serialize(imported)
                .Contains("consumer-xlsx"), "XLSX JSON reporting");
        }

        Console.WriteLine("PASS: clean NuGet net48 consumer CSV XLSX schema validation results JSON readable CSV reports.");
        return 0;
    }
}
