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

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvDuplicateIdentifierIntegrationTests
    {
        private sealed class Customer
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private static CsvImporter<Customer> CreateImporter()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<Customer>(
                mapping, CultureInfo.InvariantCulture);
            var rule = DuplicateIdentifierRule<Customer>.ForKey(
                "Id", x => x.Id, StringComparer.OrdinalIgnoreCase);
            return new CsvImporter<Customer>(
                new CsvRecordReaderAdapter(), mapper.Map, null, rule);
        }

        private static TypedImportResult<Customer> Import(
            CsvImporter<Customer> importer, string csv)
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            });
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "fr021-customers");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-021")]
        [Property("Scenario", "FR-021-SC01")]
        public void FR_021_SC01_LaterCaseInsensitiveDuplicateIsRejectedWithFieldAndRow()
        {
            var result = Import(CreateImporter(),
                "Id,Name\r\nA-1,Alice\r\na-1,private-duplicate\r\nB-2,Bob");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.SourceId, Is.EqualTo("fr021-customers"));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo("A-1"));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo("B-2"));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.DuplicateIdentifier));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Message,
                Does.Not.Contain("private-duplicate"));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("A-1"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-021")]
        [Property("Scenario", "FR-021-SC02")]
        public void FR_021_SC02_UniqueIdentifiersAndIndependentImportsAreAccepted()
        {
            var importer = CreateImporter();
            var first = Import(importer,
                "Id,Name\r\nA-1,Alice\r\nB-2,Bob");
            var second = Import(importer,
                "Id,Name\r\nA-1,Carol");

            Assert.That(first.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(first.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(first.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(first.Summary.RejectedCount, Is.Zero);
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(first.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
            Assert.That(second.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(second.Summary.ProcessedCount, Is.EqualTo(1));
            Assert.That(second.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(second.Summary.RejectedCount, Is.Zero);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(second.AcceptedRecords[0].Name, Is.EqualTo("Carol"));
        }
    }
}
