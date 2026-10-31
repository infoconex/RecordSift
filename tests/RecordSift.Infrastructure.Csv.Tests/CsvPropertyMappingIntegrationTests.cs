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
    public sealed class CsvPropertyMappingIntegrationTests
    {
        private sealed class Customer
        {
            public string CustomerId { get; set; }
            public string Name { get; set; }
        }

        private sealed class NumericCustomer
        {
            public int CustomerId { get; set; }
            public string Name { get; set; }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-010")]
        [Property("Scenario", "FR-010-SC01")]
        public void FR_010_SC01_RealCsvMapsDifferentlyNamedHeadersToTypedProperties()
        {
            var sourceSchema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", true)
            });
            var mapping = new PropertyMappingSchema(sourceSchema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Full Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<NumericCustomer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<NumericCustomer>(
                new CsvRecordReaderAdapter(), mapper.Map);

            using (var source = new StringReader(
                "Full Name,Customer Id\r\nAlice,41\r\nBob,42"))
            {
                var result = importer.Import(source, 2048, sourceSchema,
                    CsvHeaderPolicy.WithHeader, "fixture-typed");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo(41));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo(42));
                Assert.That(result.Diagnostics, Is.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-010")]
        [Property("Scenario", "FR-010-SC01")]
        public void FR_010_SC01_RealCsvReaderMapsDifferentlyNamedHeadersInSourceOrder()
        {
            var sourceSchema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", true)
            });
            var mapping = new PropertyMappingSchema(sourceSchema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Full Name", "Name")
            });
            var importer = new CsvImporter<Customer>(
                new CsvRecordReaderAdapter(), new StringPropertyMapper<Customer>(mapping).Map);
            using (var source = new StringReader(
                "Full Name,Customer Id\r\n\"Alice, Jr.\",C-1\r\nBob,C-2"))
            {
                var result = importer.Import(source, 2048, sourceSchema,
                    CsvHeaderPolicy.WithHeader, "fixture-01");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice, Jr."));
                Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo("C-1"));
                Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo("C-2"));
                Assert.That(result.Diagnostics, Is.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-017")]
        [Property("Scenario", "FR-017-SC01")]
        [Property("Scenario", "FR-017-SC02")]
        public void FR_017_SC01_SC02_RealCsvRejectsBlankRequiredValuesAndKeepsValidTypedRows()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true, true),
                new ExpectedColumn("Full Name", true, true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Full Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<NumericCustomer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<NumericCustomer>(
                new CsvRecordReaderAdapter(), mapper.Map);

            using (var source = new StringReader(
                "Full Name,Customer Id\r\nAlice,1\r\n   ,2\r\nBob,3\r\n,"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "fixture-required");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(4));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Full Name"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
                Assert.That(result.Diagnostics[1].Row, Is.EqualTo(4));
                Assert.That(result.Diagnostics[2].Row, Is.EqualTo(4));
            }
        }
    }
}
