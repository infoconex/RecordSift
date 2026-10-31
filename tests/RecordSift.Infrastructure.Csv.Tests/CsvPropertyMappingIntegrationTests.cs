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
        private static TypedImportResult<NumericCustomer> ImportNumericCustomers(
            string csv, ImportSchema schema)
        {
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Full Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<NumericCustomer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<NumericCustomer>(
                new CsvRecordReaderAdapter(), mapper.Map);
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "numeric-customers");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-009")]
        [Property("Scenario", "FR-009-SC01")]
        public void FR_009_SC01_RealCsvRecognizesRequiredColumnsRegardlessOfSourceOrder()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", true)
            });
            var result = ImportNumericCustomers(
                "Unmapped,Full Name,Customer Id\r\nignored,Alice,41\r\nunused,Bob,42",
                schema);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0], Is.TypeOf<NumericCustomer>());
            Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo(41));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo(42));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-009")]
        [Property("Scenario", "FR-009-SC02")]
        public void FR_009_SC02_RealCsvRejectsMissingRequiredHeaderBeforeImport()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", true)
            });
            var result = ImportNumericCustomers(
                "Customer Id,Other\r\n41,private-customer-name", schema);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredColumnMissing));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Full Name"));
            Assert.That(result.Diagnostics[0].Message,
                Does.Not.Contain("private-customer-name"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-014")]
        [Property("Scenario", "FR-014-SC01")]
        public void FR_014_SC01_RealCsvAliasPopulatesCanonicalMappedTypedCustomer()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true, new[] { "Client Number" }),
                new ExpectedColumn("Full Name", true)
            });
            var result = ImportNumericCustomers(
                "Full Name,Client Number\r\nAlice,41\r\nBob,42", schema);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo(41));
            Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo(42));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-014")]
        [Property("Scenario", "FR-014-SC02")]
        public void FR_014_SC02_RealCsvAliasCollisionFailsWithoutLeakingSourceValues()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true, new[] { "Client Number" }),
                new ExpectedColumn("Full Name", true)
            });
            var result = ImportNumericCustomers(
                "Customer Id,Client Number,Full Name\r\n41,99,Sensitive Customer", schema);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Customer Id"));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.AmbiguousColumn));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("Sensitive Customer"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-015")]
        [Property("Scenario", "FR-015-SC01")]
        public void FR_015_SC01_RealCsvAbsentOptionalHeaderAppliesConfiguredTypedDefault()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", false, false, "Unknown")
            });
            var result = ImportNumericCustomers("Customer Id\r\n41\r\n42", schema);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Unknown"));
            Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo(42));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-015")]
        [Property("Scenario", "FR-015-SC02")]
        public void FR_015_SC02_RealCsvPresentDataAndBlankPresentValueDoNotUseDefault()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", false, false, "Unknown")
            });
            var result = ImportNumericCustomers(
                "Customer Id,Full Name\r\n41,Alice\r\n42,", schema);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo(string.Empty));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void InvalidTypedDefaultIsRejectedWhenConfiguredPropertyMapperIsConstructed()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", false, false, "not-an-integer")
            });
            var mappings = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            Assert.Throws<ArgumentException>(() =>
                new PrimitivePropertyMapper<NumericCustomer>(mappings, CultureInfo.InvariantCulture));
        }

        private sealed class GuidCustomer
        {
            public Guid CustomerId { get; set; }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-016")]
        [Property("Scenario", "FR-016-SC01")]
        public void FR_016_SC01_RealCsvUsesExplicitGuidConverterWithoutVendorContracts()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Customer Id", true) });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            var converter = new PrimitivePropertyMapper<GuidCustomer>(
                mapping, CultureInfo.InvariantCulture,
                new[] { new FieldConversionRule("Customer Id",
                    (raw, culture) => Guid.Parse(raw)) });
            var importer = new CsvImporter<GuidCustomer>(new CsvRecordReaderAdapter(), converter.Map);
            using (var source = new StringReader(
                "Customer Id\r\n7ed046a7-a0d2-4080-bc16-578f7c8083fe"))
            {
                var result = importer.Import(source, 2048, schema,
                    CsvHeaderPolicy.WithHeader, "guid-source");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].CustomerId,
                    Is.EqualTo(Guid.Parse("7ed046a7-a0d2-4080-bc16-578f7c8083fe")));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-016")]
        [Property("Scenario", "FR-016-SC02")]
        public void FR_016_SC02_RealCsvRejectedConversionKeepsSourceLineAndLaterRecord()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Customer Id", true) });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            var converter = new PrimitivePropertyMapper<GuidCustomer>(
                mapping, CultureInfo.InvariantCulture,
                new[] { new FieldConversionRule("Customer Id",
                    (raw, culture) => Guid.Parse(raw)) });
            var importer = new CsvImporter<GuidCustomer>(new CsvRecordReaderAdapter(), converter.Map);
            using (var source = new StringReader(
                "Customer Id\r\nsensitive-not-a-guid\r\n7ed046a7-a0d2-4080-bc16-578f7c8083fe"))
            {
                var result = importer.Import(source, 2048, schema,
                    CsvHeaderPolicy.WithHeader, "guid-source");
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Customer Id"));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("sensitive-not-a-guid"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-023")]
        [Property("Scenario", "FR-023-SC01")]
        public void FR_023_SC01_RealCsvPatternViolationRejectsOnlyInvalidRowWithProvenance()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", true)
            }, new[] { new FieldPatternRule("Full Name", "^[A-Z][a-z]+$") });
            var result = ImportNumericCustomers(
                "Customer Id,Full Name\r\n1,Alice\r\n2,private-invalid\r\n3,Bob", schema);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Full Name"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-invalid"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-023")]
        [Property("Scenario", "FR-023-SC02")]
        public void FR_023_SC02_RealCsvMatchingPatternPreservesAllValidTypedRecords()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Full Name", true)
            }, new[] { new FieldPatternRule("Full Name", "^[A-Z][a-z]+$") });
            var result = ImportNumericCustomers(
                "Customer Id,Full Name\r\n1,Alice\r\n2,Bob", schema);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Empty);
        }

    }
}
