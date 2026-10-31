using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
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
    }
}
