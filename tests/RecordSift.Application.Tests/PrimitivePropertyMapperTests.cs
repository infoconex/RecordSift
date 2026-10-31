using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    public sealed class PrimitivePropertyMapperTests
    {
        private sealed class Customer
        {
            public int CustomerId { get; set; }
            public decimal Amount { get; set; }
            public bool Active { get; set; }
            public int? Score { get; set; }
        }

        private sealed class FakeReader : ICsvRecordReader
        {
            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                return CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Customer Id", "Amount", "Active", "Score" }),
                    new CsvRecord(2, 2, new[] { "invalid-id", "1.5", "true", "" }),
                    new CsvRecord(3, 3, new[] { "2", "2.5", "false", "7" })
                });
            }
        }

        private static PropertyMappingSchema Schema()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Amount", true),
                new ExpectedColumn("Active", true),
                new ExpectedColumn("Score", false)
            });
            return new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Active", "Active"),
                new SourcePropertyMapping("Score", "Score")
            });
        }

        [Test]
        public void FR_011_SC01_FakeReaderImportsTypedValuesAndClassifiesBadField()
        {
            var mapping = Schema();
            var converter = new PrimitivePropertyMapper<Customer>(mapping,
                CultureInfo.InvariantCulture);
            var importer = new CsvImporter<Customer>(new FakeReader(), converter.Map);
            using (var input = new StringReader("unused"))
            {
                var result = importer.Import(input, 4096, mapping.SourceSchema,
                    CsvHeaderPolicy.WithHeader, "customers");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Customer Id"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(2.5m));
                Assert.That(result.AcceptedRecords[0].Active, Is.False);
                Assert.That(result.AcceptedRecords[0].Score, Is.EqualTo(7));
            }
        }

        [Test]
        public void FR_010_SourceIdentifiersRemainOrdinalWhenInputDictionaryIgnoresCase()
        {
            var mapper = new PrimitivePropertyMapper<Customer>(
                Schema(), CultureInfo.InvariantCulture);
            var values = new System.Collections.Generic.Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase) {
                    { "customer id", "10" },
                    { "Amount", "2.5" },
                    { "Active", "true" },
                    { "Score", "" }
                };

            Assert.Throws<ArgumentException>(() => mapper.Map(values));
        }
    }
}
