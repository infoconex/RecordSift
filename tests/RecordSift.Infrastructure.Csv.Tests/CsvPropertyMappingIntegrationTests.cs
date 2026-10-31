using System;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    public sealed class CsvPropertyMappingIntegrationTests
    {
        private sealed class Customer
        {
            public string CustomerId { get; set; }
            public string Name { get; set; }
        }

        [Test]
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
    }
}
