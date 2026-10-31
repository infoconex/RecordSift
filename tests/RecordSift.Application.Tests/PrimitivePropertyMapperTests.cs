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
    [Category("Unit")]
    public sealed class PrimitivePropertyMapperTests
    {
        private sealed class Customer
        {
            public int CustomerId { get; set; }
            public decimal Amount { get; set; }
            public bool Active { get; set; }
            public int? Score { get; set; }
        }

        private sealed class UnsupportedDestination
        {
            public Guid CustomerId { get; set; }
        }

        private sealed class ReadOnlyDestination
        {
            public int CustomerId { get; private set; }
        }


        private sealed class RejectingCustomer
        {
            public int CustomerId { get; set; }
            private string _name;

            public string Name
            {
                get { return _name; }
                set
                {
                    if (value == "reject-me")
                        throw new InvalidOperationException("private-setter-detail");
                    _name = value;
                }
            }
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

        private sealed class Order
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime OrderDate { get; set; }
        }

        private sealed class ScenarioReader : ICsvRecordReader
        {
            private readonly CsvParseResult _records;

            public ScenarioReader(CsvParseResult records)
            {
                _records = records;
            }

            public CsvParseResult Read(TextReader source, int maxCharacters,
                CsvReadOptions options = null)
            {
                return _records;
            }
        }

        private static TypedImportResult<Order> ImportOrders(CsvRecord[] records)
        {
            var sourceSchema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Amount", true),
                new ExpectedColumn("Order Date", true)
            });
            var mapping = new PropertyMappingSchema(sourceSchema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Order Date", "OrderDate")
            });
            var mapper = new PrimitivePropertyMapper<Order>(
                mapping, CultureInfo.GetCultureInfo("fr-FR"));
            var importer = new CsvImporter<Order>(new ScenarioReader(
                CsvParseResult.Success(records)), mapper.Map);

            using (var input = new StringReader("unused"))
                return importer.Import(input, 4096, sourceSchema,
                    CsvHeaderPolicy.WithHeader, "orders-application");
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
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-011")]
        [Property("Scenario", "FR-011-SC01")]
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
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-018")]
        [Property("Scenario", "FR-018-SC01")]
        public void FR_018_SC01_FakeReaderRejectsInvalidNumericAndDateRowsIndependently()
        {
            var result = ImportOrders(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Amount", "Order Date" }),
                new CsvRecord(2, 2, new[] { "1", "12,50", "31/12/2025" }),
                new CsvRecord(3, 3, new[] { "2", "not-a-number", "01/01/2025" }),
                new CsvRecord(4, 4, new[] { "3", "2,50", "not-a-date" }),
                new CsvRecord(5, 5, new[] { "4", "7,25", "02/01/2025" })
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(4));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(4));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Order Date"));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].PhysicalLine, Is.EqualTo(4));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("not-a-number"));
            Assert.That(result.Diagnostics[1].Message, Does.Not.Contain("not-a-date"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-018")]
        [Property("Scenario", "FR-018-SC02")]
        public void FR_018_SC02_FakeReaderPreservesValidTypedValuesInExplicitCulture()
        {
            var result = ImportOrders(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Amount", "Order Date" }),
                new CsvRecord(2, 2, new[] { "1", "12,50", "31/12/2025" }),
                new CsvRecord(3, 3, new[] { "2", "-7,25", "01/01/2026" })
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.50m));
            Assert.That(result.AcceptedRecords[0].OrderDate,
                Is.EqualTo(new DateTime(2025, 12, 31)));
            Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(-7.25m));
            Assert.That(result.AcceptedRecords[1].OrderDate,
                Is.EqualTo(new DateTime(2026, 1, 1)));
        }

        [Test]
        [Property("Requirement", "FR-010")]
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

        [Test]
        [Property("Requirement", "FR-010")]
        public void FR_010_OptionalSourceDoesNotMatchDifferentlyCasedCallerKey()
        {
            var mapper = new PrimitivePropertyMapper<Customer>(
                Schema(), CultureInfo.InvariantCulture);
            var values = new System.Collections.Generic.Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase) {
                    { "Customer Id", "10" },
                    { "Amount", "2.5" },
                    { "Active", "true" },
                    { "score", "7" }
                };

            var record = mapper.Map(values);
            Assert.That(record.CustomerId, Is.EqualTo(10));
            Assert.That(record.Score, Is.Null);
        }

        [Test]
        [Property("Requirement", "FR-010")]
        public void FR_010_UnsupportedReadOnlyAndWrongCaseDestinationPropertiesFailBeforeMapping()
        {
            var sourceSchema = Schema().SourceSchema;
            var validName = new PropertyMappingSchema(sourceSchema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            var wrongCase = new PropertyMappingSchema(sourceSchema, new[] {
                new SourcePropertyMapping("Customer Id", "customerId")
            });

            Assert.Throws<ArgumentException>(() => new PrimitivePropertyMapper<Customer>(
                wrongCase, CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentException>(() => new PrimitivePropertyMapper<UnsupportedDestination>(
                validName, CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentException>(() => new PrimitivePropertyMapper<ReadOnlyDestination>(
                validName, CultureInfo.InvariantCulture));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-026")]
        [Property("Scenario", "FR-026-SC02")]
        public void FR_026_SC02_FakeReaderClassifiesRejectedDestinationSetterByField()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Name", true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<RejectingCustomer>(
                mapping, CultureInfo.InvariantCulture);
            var reader = new ScenarioReader(CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Customer Id", "Name" }),
                new CsvRecord(2, 2, new[] { "1", "Alice" }),
                new CsvRecord(3, 3, new[] { "2", "reject-me" }),
                new CsvRecord(4, 4, new[] { "3", "Bob" })
            }));
            var importer = new CsvImporter<RejectingCustomer>(reader, mapper.Map);

            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "customers-fake");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo(3));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("reject-me"));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-setter-detail"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-026")]
        [Property("Scenario", "FR-026-SC02")]
        public void FR_026_SC02_RejectsUnsupportedAndUnwritableDestinationsBeforeImport()
        {
            var schema = Schema().SourceSchema;
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });

            Assert.Throws<ArgumentException>(() =>
                new PrimitivePropertyMapper<UnsupportedDestination>(
                    mapping, CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentException>(() =>
                new PrimitivePropertyMapper<ReadOnlyDestination>(
                    mapping, CultureInfo.InvariantCulture));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-027")]
        [Property("Scenario", "FR-027-SC01")]
        public void FR_027_SC01_FakeReaderKeepsValidOrdersInSourceOrderAcrossRejectedRows()
        {
            var result = ImportOrders(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Amount", "Order Date" }),
                new CsvRecord(2, 2, new[] { "1", "1,50", "31/12/2025" }),
                new CsvRecord(3, 3, new[] { "invalid-id", "2,50", "31/12/2025" }),
                new CsvRecord(4, 4, new[] { "3", "3,50", "01/01/2026" }),
                new CsvRecord(5, 5, new[] { "4", "not-a-number", "01/01/2026" }),
                new CsvRecord(6, 6, new[] { "5", "5,50", "02/01/2026" })
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(5));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(3));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(3));
            Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo(5));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(4));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-027")]
        [Property("Scenario", "FR-027-SC02")]
        public void FR_027_SC02_FakeReaderExcludesEveryRejectedOrder()
        {
            var result = ImportOrders(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Amount", "Order Date" }),
                new CsvRecord(2, 2, new[] { "invalid-id", "1,50", "31/12/2025" }),
                new CsvRecord(3, 3, new[] { "2", "invalid-amount", "01/01/2026" })
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Amount"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-029")]
        [Property("Scenario", "FR-029-SC01")]
        public void FR_029_SC01_FakeReaderContinuesAfterRecordErrorWithValidLaterRecords()
        {
            var result = ImportOrders(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Amount", "Order Date" }),
                new CsvRecord(2, 2, new[] { "1", "12,50", "31/12/2025" }),
                new CsvRecord(3, 3, new[] { "2", "invalid-amount", "01/01/2026" }),
                new CsvRecord(4, 4, new[] { "3", "7,25", "02/01/2026" })
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(3));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("invalid-amount"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-029")]
        [Property("Scenario", "FR-029-SC02")]
        public void FR_029_SC02_FakeReaderReportsUnreadableSourceAsFailedOperation()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Amount", true),
                new ExpectedColumn("Order Date", true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Order Date", "OrderDate")
            });
            var mapper = new PrimitivePropertyMapper<Order>(mapping,
                CultureInfo.GetCultureInfo("fr-FR"));
            var reader = new ScenarioReader(CsvParseResult.Failure(
                ImportDiagnostic.ForSource(null, DiagnosticCodes.SourceUnreadable)));
            var importer = new CsvImporter<Order>(reader, mapper.Map);

            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "unreadable-fake");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.SourceId, Is.EqualTo("unreadable-fake"));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Summary.AcceptedCount, Is.Zero);
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceUnreadable));
                Assert.That(result.Diagnostics[0].Row, Is.Null);
                Assert.That(result.Message, Is.Not.Empty);
            }
        }
        private sealed class CustomGuidCustomer
        {
            public Guid CustomerId { get; set; }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-016")]
        [Property("Scenario", "FR-016-SC01")]
        public void FR_016_SC01_FakeReaderUsesRegisteredConverterForUnsupportedGuidDestination()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Customer Id", true) });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            var converter = new PrimitivePropertyMapper<CustomGuidCustomer>(
                mapping, CultureInfo.InvariantCulture,
                new[] { new FieldConversionRule("Customer Id",
                    (raw, culture) => Guid.Parse(raw)) });
            var importer = new CsvImporter<CustomGuidCustomer>(new ScenarioReader(
                CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Customer Id" }),
                    new CsvRecord(2, 2, new[] { "7ed046a7-a0d2-4080-bc16-578f7c8083fe" })
                })), converter.Map);

            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 2048, schema,
                    CsvHeaderPolicy.WithHeader, "guid-source");
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(result.AcceptedRecords[0].CustomerId,
                    Is.EqualTo(Guid.Parse("7ed046a7-a0d2-4080-bc16-578f7c8083fe")));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-016")]
        [Property("Scenario", "FR-016-SC02")]
        public void FR_016_SC02_CustomConverterRejectionIsSanitizedAndNextRowAccepted()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Customer Id", true) });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            var converter = new PrimitivePropertyMapper<CustomGuidCustomer>(
                mapping, CultureInfo.InvariantCulture,
                new[] { new FieldConversionRule("Customer Id", (raw, culture) =>
                {
                    if (raw == "secret-invalid")
                        throw new FormatException("private-converter-detail " + raw);
                    return Guid.Parse(raw);
                }) });
            var importer = new CsvImporter<CustomGuidCustomer>(new ScenarioReader(
                CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Customer Id" }),
                    new CsvRecord(2, 2, new[] { "secret-invalid" }),
                    new CsvRecord(3, 3, new[] { "7ed046a7-a0d2-4080-bc16-578f7c8083fe" })
                })), converter.Map);

            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 2048, schema,
                    CsvHeaderPolicy.WithHeader, "guid-source");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Customer Id"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("secret-invalid"));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-converter-detail"));
            }
        }

        [Test]
        public void DuplicateUnknownAndNullCustomConverterRegistrationsAreRejected()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Customer Id", true) });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            });
            var rule = new FieldConversionRule("Customer Id", (raw, culture) => Guid.Parse(raw));
            Assert.Throws<ArgumentException>(() => new PrimitivePropertyMapper<CustomGuidCustomer>(
                mapping, CultureInfo.InvariantCulture, new[] { rule, rule }));
            Assert.Throws<ArgumentException>(() => new PrimitivePropertyMapper<CustomGuidCustomer>(
                mapping, CultureInfo.InvariantCulture,
                new[] { new FieldConversionRule("Unknown", (raw, culture) => Guid.Parse(raw)) }));
            Assert.Throws<ArgumentException>(() => new PrimitivePropertyMapper<CustomGuidCustomer>(
                mapping, CultureInfo.InvariantCulture, new FieldConversionRule[] { null }));
        }

    }
}
