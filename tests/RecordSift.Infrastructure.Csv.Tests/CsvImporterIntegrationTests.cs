using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvImporterIntegrationTests
    {
        private sealed class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Id", true), new ExpectedColumn("Name", true)
            });
        }

        private static TypedImportResult<Customer> Import(string csv,
            CsvHeaderPolicy policy = CsvHeaderPolicy.WithHeader)
        {
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                values => new Customer {
                    Id = int.Parse(values["Id"], NumberStyles.Integer,
                        CultureInfo.InvariantCulture),
                    Name = values["Name"]
                });
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, Schema(), policy, "fixture-01");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC01")]
        [Property("Scenario", "FR-001-SC04")]
        public void FR_001_SC01_SC04_RealCsvImportReturnsThreeTypedOrderedQuotedCustomers()
        {
            var result = Import("Id,Name\r\n1,\"Alice, Jr.\"\r\n2,\"Bob\r\nTwo\"\r\n3,Carol");
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice, Jr."));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob\r\nTwo"));
            Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo(3));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC02")]
        [Property("Scenario", "FR-001-SC05")]
        [Property("Scenario", "FR-001-SC06")]
        public void FR_001_SC02_SC05_SC06_RealCsvImportDistinguishesEmptyOutcomes()
        {
            var headed = Import("Id,Name\r\n");
            var headerless = Import("", CsvHeaderPolicy.WithoutHeader);
            var missing = Import("");

            Assert.That(headed.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headed.Summary.ProcessedCount, Is.Zero);
            Assert.That(headed.Diagnostics, Is.Empty);
            Assert.That(headed.Message, Does.Contain("No data records"));
            Assert.That(headerless.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headerless.Diagnostics, Is.Empty);
            Assert.That(headerless.Message, Does.Contain("no records").IgnoreCase);
            Assert.That(missing.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(missing.Summary.ProcessedCount, Is.Zero);
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC03")]
        public void FR_001_SC03_RealMalformedCsvFailsAtomicallyWithLocation()
        {
            var result = Import("Id,Name\r\n1,Alice\r\n2,\"unfinished");
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
        }

        [Test]
        public void RealCsvConversionErrorRejectsOneRecordWithoutLeakingSourceValues()
        {
            var result = Import("Id,Name\nnot-an-id,Secret Customer\n2,Bob");
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Bob"));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("Secret Customer"));
        }
    }
}
