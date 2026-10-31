using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;
using RecordSift.Infrastructure.Reporting;

namespace RecordSift.Infrastructure.Reporting.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvReadableReportIntegrationTests
    {
        private sealed class Customer
        {
            public string Id { get; set; }
        }

        private static TypedImportResult<Customer> Import(string csv, CsvHeaderPolicy policy)
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                values => new Customer { Id = values["Id"] });
            using (var reader = new StringReader(csv))
                return importer.Import(reader, 4096, schema, policy, "empty-csv");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-037")]
        [Property("Scenario", "FR-037-SC03")]
        public void FR_037_SC03_RealCsvEmptyHeaderPoliciesProduceDistinctReadableOutcomes()
        {
            var headedEmpty = Import("Id\r\n", CsvHeaderPolicy.WithHeader);
            var headerlessEmpty = Import(string.Empty, CsvHeaderPolicy.WithoutHeader);
            var missingHeader = Import(string.Empty, CsvHeaderPolicy.WithHeader);
            var formatter = new ReadableImportResultFormatterAdapter();

            foreach (var success in new[] { headedEmpty, headerlessEmpty })
            {
                Assert.That(success.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(success.Summary.ProcessedCount, Is.Zero);
                Assert.That(success.Summary.RejectedCount, Is.Zero);
                Assert.That(success.Diagnostics, Is.Empty);
                var report = formatter.Format(success);
                Assert.That(report, Does.Contain("No data records found"));
                Assert.That(report, Does.Not.Contain("Import failed"));
            }

            Assert.That(missingHeader.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(missingHeader.Summary.ProcessedCount, Is.Zero);
            Assert.That(missingHeader.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(missingHeader.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
            var failure = formatter.Format(missingHeader);
            Assert.That(failure, Does.Contain("Import failed"));
            Assert.That(failure, Does.Contain("required source header is missing"));
            Assert.That(failure, Does.Not.Contain("No data records found"));
        }

        private sealed class DeterministicRecord
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime EventDate { get; set; }
            public string Name { get; set; }
        }

        private static TypedImportResult<DeterministicRecord> ImportDeterministicCsv()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Amount", true),
                new ExpectedColumn("Event Date", true),
                new ExpectedColumn("Name", true)
            });
            var mappings = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Event Date", "EventDate"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<DeterministicRecord>(
                mappings, CultureInfo.GetCultureInfo("fr-FR"));
            var range = FieldRangeRule<DeterministicRecord>.Numeric(
                "Amount", record => record.Amount, 1m, true, 20m, true);
            var unique = DuplicateIdentifierRule<DeterministicRecord>.ForKey(
                "Id", record => record.Id);
            var allowed = new RecordValidationRule<DeterministicRecord>(
                "Name", record => record.Name != "Blocked");
            var importer = new CsvImporter<DeterministicRecord>(new CsvRecordReaderAdapter(),
                mapper.Map, new[] { range }, unique, new[] { allowed });
            var csv = "Id;Amount;Event Date;Name\n" +
                "1;12,50;31/12/2025;Alice\n" +
                "2;15,00;01/01/2026;Bob\n" +
                "2;11,00;02/01/2026;Duplicate\n" +
                "3;0,50;03/01/2026;TooLow\n" +
                "4;7,25;04/01/2026;Blocked\n" +
                "5;bad;05/01/2026;NotNumber\n" +
                "6;5,00;06/01/2026;Eve";
            using (var reader = new StringReader(csv))
                return importer.Import(reader, 8192, schema,
                    CsvHeaderPolicy.WithHeader, "deterministic-csv",
                    options: new RecordSift.Application.Ports.Csv.CsvReadOptions(";"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-004")]
        [Property("Scenario", "NFR-004-SC01")]
        public void NFR_004_SC01_RepeatedCsvImportsPreserveOrderedRecordsDiagnosticsAndReports()
        {
            var jsonAdapter = new JsonImportResultSerializerAdapter();
            var readableAdapter = new ReadableImportResultFormatterAdapter();
            string expectedJson = null;
            string expectedReadable = null;
            for (var repeat = 0; repeat < 8; repeat++)
            {
                var result = ImportDeterministicCsv();
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(7));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(4));
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.5m));
                Assert.That(result.AcceptedRecords[0].EventDate,
                    Is.EqualTo(new DateTime(2025, 12, 31)));
                Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo(6));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
                var codes = new[] {
                    DiagnosticCodes.DuplicateIdentifier, DiagnosticCodes.FieldRangeOutOfRange,
                    DiagnosticCodes.ValidationFailed, DiagnosticCodes.ConversionFailed
                };
                for (var index = 0; index < codes.Length; index++)
                {
                    Assert.That(result.Diagnostics[index].Code, Is.EqualTo(codes[index]));
                    Assert.That(result.Diagnostics[index].Row, Is.EqualTo(index + 3));
                    Assert.That(result.Diagnostics[index].PhysicalLine, Is.EqualTo(index + 4));
                }

                var json = jsonAdapter.Serialize(result);
                var readable = readableAdapter.Format(result);
                if (expectedJson == null)
                {
                    expectedJson = json;
                    expectedReadable = readable;
                }
                Assert.That(json, Is.EqualTo(expectedJson));
                Assert.That(readable, Is.EqualTo(expectedReadable));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-004")]
        [Property("Scenario", "NFR-004-SC02")]
        public void NFR_004_SC02_ExplicitFrenchParsingAndReportsIgnoreAmbientCulture()
        {
            var jsonAdapter = new JsonImportResultSerializerAdapter();
            var readableAdapter = new ReadableImportResultFormatterAdapter();
            var baseline = ImportDeterministicCsv();
            var expectedJson = jsonAdapter.Serialize(baseline);
            var expectedReadable = readableAdapter.Format(baseline);
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var cultureName in new[] { "en-US", "fr-FR", "de-DE" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                    var result = ImportDeterministicCsv();
                    Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.5m));
                    Assert.That(result.AcceptedRecords[0].EventDate,
                        Is.EqualTo(new DateTime(2025, 12, 31)));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
                    Assert.That(result.Summary.RejectedCount, Is.EqualTo(4));
                    Assert.That(jsonAdapter.Serialize(result), Is.EqualTo(expectedJson));
                    Assert.That(readableAdapter.Format(result), Is.EqualTo(expectedReadable));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }
}
