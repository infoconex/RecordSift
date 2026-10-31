using System;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class StringPropertyMapperTests
    {
        private sealed class Customer
        {
            public string CustomerId { get; set; }
            public string DisplayName { get; set; }
        }

        private sealed class UnsupportedCustomer
        {
            public int CustomerId { get; set; }
        }

        private sealed class ReadOnlyCustomer
        {
            public string CustomerId { get; private set; }
        }

        private sealed class FakeReader : ICsvRecordReader
        {
            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                return CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Display Name", "Customer Id" }),
                    new CsvRecord(2, 2, new[] { "Alice", "C-01" })
                });
            }
        }

        private static PropertyMappingSchema Mapping()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Display Name", true)
            });
            return new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Display Name", "DisplayName")
            });
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-010")]
        [Property("Scenario", "FR-010-SC01")]
        public void FR_010_SC01_DifferentlyNamedSourceColumnMapsToExplicitClrProperty()
        {
            var mapper = new StringPropertyMapper<Customer>(Mapping());
            var importer = new CsvImporter<Customer>(new FakeReader(), mapper.Map);
            using (var input = new StringReader("unused"))
            {
                var result = importer.Import(input, 100, Mapping().SourceSchema,
                    CsvHeaderPolicy.WithHeader, "customers");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo("C-01"));
                Assert.That(result.AcceptedRecords[0].DisplayName, Is.EqualTo("Alice"));
            }
        }

        [Test]
        public void NonStringOrNonPublicSetterIsRejectedAtConfigurationTime()
        {
            var schema = Mapping();
            Assert.Throws<ArgumentException>(() => new StringPropertyMapper<UnsupportedCustomer>(
                new PropertyMappingSchema(schema.SourceSchema, new[] {
                    new SourcePropertyMapping("Customer Id", "CustomerId")
                })));
            Assert.Throws<ArgumentException>(() => new StringPropertyMapper<ReadOnlyCustomer>(
                new PropertyMappingSchema(schema.SourceSchema, new[] {
                    new SourcePropertyMapping("Customer Id", "CustomerId")
                })));
        }

        [Test]
        public void MissingRequiredMappedSourceCannotSilentlyCreateAnIncompleteRecord()
        {
            var mapper = new StringPropertyMapper<Customer>(Mapping());
            Assert.Throws<ArgumentException>(() => mapper.Map(
                new System.Collections.Generic.Dictionary<string, string>()));
        }

        [Test]
        public void UnavailableOptionalSourceColumnLeavesCorrespondingPropertyUnset()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Alias", false) });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Alias", "DisplayName")
            });
            var record = new StringPropertyMapper<Customer>(mapping).Map(
                new System.Collections.Generic.Dictionary<string, string>());
            Assert.That(record.DisplayName, Is.Null);
        }

        [Test]
        public void MissingDestinationPropertyIsRejectedWithoutReadingRecords()
        {
            var schema = Mapping();
            Assert.Throws<ArgumentException>(() => new StringPropertyMapper<Customer>(
                new PropertyMappingSchema(schema.SourceSchema, new[] {
                    new SourcePropertyMapping("Customer Id", "UnknownMember")
                })));
        }
    }
}
