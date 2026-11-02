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

// Compiled as a separate C# 7.3 executable against independently installed RecordSift DLLs.
// Verifies the four actual downloadable sample files used in Getting Started.
internal static class GettingStartedSamples
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

    private static void Require(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException("Getting Started sample check: " + description);
    }

    private static ImportSchema CreateSchema()
    {
        return new ImportSchema(new[]
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
    }

    private static PropertyMappingSchema CreateMapping(ImportSchema importSchema)
    {
        return new PropertyMappingSchema(importSchema, new[]
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
    }

    private static void CheckResult(TypedImportResult<Customer> result,
        string source, bool invalid)
    {
        Require(result.Status == ImportStatus.Completed, source + " import completed");
        Require(result.Summary.ProcessedCount == (invalid ? 10 : 20), source + " processed count");
        Require(result.Summary.AcceptedCount == (invalid ? 2 : 20), source + " accepted count");
        Require(result.Summary.RejectedCount == (invalid ? 8 : 0), source + " rejected count");
        Require(result.AcceptedRecords.Count == result.Summary.AcceptedCount,
            source + " accepted records");
        Require(invalid ? result.Diagnostics.Count >= 8 : result.Diagnostics.Count == 0,
            source + " record diagnostics");
        if (!invalid)
        {
            Require(result.AcceptedRecords[0].Id == 1001, source + " first customer ID");
            Require(result.AcceptedRecords[0].Name == "John Smith", source + " first customer name");
            Require(result.AcceptedRecords[0].CreditLimit == 5000.00m, source + " decimal conversion");
            Require(result.AcceptedRecords[0].IsActive, source + " Boolean conversion");
            Require(result.AcceptedRecords[0].LastPurchaseDate.HasValue, source + " populated optional date");
            Require(!result.AcceptedRecords[3].LastPurchaseDate.HasValue, source + " blank nullable date");
            Require(string.IsNullOrEmpty(result.AcceptedRecords[3].ReferralCode), source + " blank optional string");
            Require(result.AcceptedRecords[19].Id == 1020, source + " last customer ID");
        }
        else
        {
            Require(result.AcceptedRecords[0].Id == 2009, source + " malformed email is not automatically validated");
            Require(result.AcceptedRecords[1].Id == 2010, source + " negative points require a business rule");
        }
    }

    private static void CompareResults(TypedImportResult<Customer> csv,
        TypedImportResult<Customer> xlsx, string scenario)
    {
        Require(csv.Status == xlsx.Status, scenario + " status parity");
        Require(csv.Summary.ProcessedCount == xlsx.Summary.ProcessedCount, scenario + " processed parity");
        Require(csv.Summary.AcceptedCount == xlsx.Summary.AcceptedCount, scenario + " accepted parity");
        Require(csv.Summary.RejectedCount == xlsx.Summary.RejectedCount, scenario + " rejected parity");
        Require(csv.Diagnostics.Count == xlsx.Diagnostics.Count, scenario + " diagnostics count parity");
        for (var i = 0; i < csv.AcceptedRecords.Count; i++)
        {
            var a = csv.AcceptedRecords[i];
            var b = xlsx.AcceptedRecords[i];
            Require(a.Id == b.Id && a.Name == b.Name && a.Email == b.Email
                && a.RegistrationDate == b.RegistrationDate && a.LoyaltyPoints == b.LoyaltyPoints
                && a.CreditLimit == b.CreditLimit && a.IsActive == b.IsActive
                && a.CustomerTier == b.CustomerTier && a.LastPurchaseDate == b.LastPurchaseDate
                && (a.ReferralCode == b.ReferralCode ||
                    (string.IsNullOrEmpty(a.ReferralCode) && string.IsNullOrEmpty(b.ReferralCode))),
                scenario + " record parity at " + i);
        }
        for (var i = 0; i < csv.Diagnostics.Count; i++)
        {
            var a = csv.Diagnostics[i];
            var b = xlsx.Diagnostics[i];
            Require(a.Code == b.Code && a.Scope == b.Scope
                && a.Row == b.Row && a.Field == b.Field,
                scenario + " diagnostic parity at " + i);
        }
    }

    public static int Main(string[] args)
    {
        Require(args.Length == 1 && Directory.Exists(args[0]), "sample directory argument");
        var directory = args[0];
        var importSchema = CreateSchema();
        var propertyMappingSchema = CreateMapping(importSchema);
        var primitivePropertyMapper = new PrimitivePropertyMapper<Customer>(
            propertyMappingSchema, CultureInfo.InvariantCulture);
        var csvImporter = new CsvImporter<Customer>(
            new CsvRecordReaderAdapter(), primitivePropertyMapper.Map);
        var xlsxImporter = new XlsxImporter<Customer>(
            new XlsxWorksheetReaderAdapter(), primitivePropertyMapper.Map);

        foreach (var scenario in new[] { "customers", "customers-invalid" })
        {
            var invalid = scenario.EndsWith("-invalid", StringComparison.Ordinal);
            TypedImportResult<Customer> csvResult;
            TypedImportResult<Customer> xlsxResult;

            using (var streamReader = File.OpenText(Path.Combine(directory, scenario + ".csv")))
                csvResult = csvImporter.Import(streamReader, 4096, importSchema,
                    CsvHeaderPolicy.WithHeader, scenario, options: CsvReadOptions.Default);

            using (var fileStream = File.OpenRead(Path.Combine(directory, scenario + ".xlsx")))
                xlsxResult = xlsxImporter.Import(fileStream, 4 * 1024 * 1024,
                    importSchema, scenario, limits: XlsxReadLimits.Default);

            CheckResult(csvResult, scenario + ".csv", invalid);
            CheckResult(xlsxResult, scenario + ".xlsx", invalid);
            CompareResults(csvResult, xlsxResult, scenario);
            Console.WriteLine("PASS: " + scenario + " — CSV and XLSX imports, typed records, diagnostics and parity.");
        }
        return 0;
    }
}
