using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class CsvImporterTests
    {
        private sealed class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private sealed class FakeReader : ICsvRecordReader
        {
            private readonly CsvParseResult _result;

            public FakeReader(CsvParseResult result)
            {
                _result = result;
            }

            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                return _result;
            }
        }

        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Name", true)
            });
        }

        private static Customer MapCustomer(IReadOnlyDictionary<string, string> values)
        {
            return new Customer {
                Id = int.Parse(values["Id"], NumberStyles.Integer, CultureInfo.InvariantCulture),
                Name = values["Name"]
            };
        }

        private static TypedImportResult<Customer> Import(CsvParseResult raw,
            CsvHeaderPolicy policy = CsvHeaderPolicy.WithHeader)
        {
            var importer = new CsvImporter<Customer>(new FakeReader(raw), MapCustomer);
            using (var source = new StringReader("unused"))
                return importer.Import(source, 4096, Schema(), policy, "customers-01");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC01")]
        public void FR_001_SC01_ValidCsvProducesOrderedTypedCustomersAndSummary()
        {
            var result = Import(CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Name", "Id" }),
                new CsvRecord(2, 2, new[] { "Alice", "1" }),
                new CsvRecord(3, 3, new[] { "Bob", "2" }),
                new CsvRecord(4, 4, new[] { "Carol", "3" })
            }));

            Assert.That(result.SourceId, Is.EqualTo("customers-01"));
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[2].Name, Is.EqualTo("Carol"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC02")]
        public void FR_001_SC02_HeaderOnlySourceCompletesWithInformationalMessage()
        {
            var result = Import(CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Name" })
            }));

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Message, Does.Contain("No data records"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC05")]
        public void FR_001_SC05_EmptyHeaderlessSourceCompletesWithoutErrors()
        {
            var result = Import(CsvParseResult.Success(new CsvRecord[0]),
                CsvHeaderPolicy.WithoutHeader);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Message, Does.Contain("no records").IgnoreCase);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC06")]
        public void FR_001_SC06_EmptyHeadedSourceFailsWithRequiredHeaderCode()
        {
            var result = Import(CsvParseResult.Success(new CsvRecord[0]));

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
            Assert.That(result.Message, Does.Contain("header"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC03")]
        public void FR_001_SC03_MalformedSourceFailureKeepsPhysicalLineAndNoPartialRecords()
        {
            var error = ImportDiagnostic.ForSource(null, DiagnosticCodes.MalformedSource,
                null, null, 3);
            var result = Import(CsvParseResult.Failure(error));

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
        }

        [Test]
        public void InvalidRecordConversionIsClassifiedAndFollowingRowsRemainAccepted()
        {
            var result = Import(CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Name" }),
                new CsvRecord(2, 2, new[] { "invalid-id", "Secret Alice" }),
                new CsvRecord(3, 3, new[] { "2", "Bob" })
            }));

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Bob"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("Secret Alice"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-017")]
        [Property("Scenario", "FR-017-SC01")]
        [Property("Scenario", "FR-017-SC02")]
        public void FR_017_SC01_SC02_RequiredValueFailuresRejectOnlyTheirOwnRows()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            });
            var raw = CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Name" }),
                new CsvRecord(2, 2, new[] { "1", "Alice" }),
                new CsvRecord(3, 3, new[] { "2", "   " }),
                new CsvRecord(4, 4, new[] { "3", "Carol" }),
                new CsvRecord(5, 5, new[] { "", "" })
            });
            var importer = new CsvImporter<Customer>(new FakeReader(raw), MapCustomer);
            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "customers");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(4));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Carol"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
                Assert.That(result.Diagnostics[1].Row, Is.EqualTo(4));
                Assert.That(result.Diagnostics[2].Row, Is.EqualTo(4));
            }
        }
        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-015")]
        [Property("Scenario", "FR-015-SC01")]
        public void FR_015_SC01_ApplicationFakeReaderAppliesDefaultOnlyToMissingOptionalField()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Nickname", false, false, "Unknown")
            });
            var raw = CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Id" }),
                new CsvRecord(2, 2, new[] { "1" })
            });
            var importer = new CsvImporter<Customer>(new FakeReader(raw),
                values => new Customer {
                    Id = int.Parse(values["Id"], CultureInfo.InvariantCulture),
                    Name = values["Nickname"]
                });
            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 1024, schema,
                    CsvHeaderPolicy.WithHeader, "default-source");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Unknown"));
                Assert.That(result.Diagnostics, Is.Empty);
            }
        }


        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-023")]
        [Property("Scenario", "FR-023-SC01")]
        public void FR_023_SC01_FakeReaderRegexRejectsBeforeConversionAndKeepsSubsequentRecords()
        {
            var raw = CsvParseResult.Success(new[] {
                new CsvRecord(1, 1, new[] { "Id", "Name" }),
                new CsvRecord(2, 2, new[] { "1", "Alice" }),
                new CsvRecord(3, 5, new[] { "2", "private-invalid" }),
                new CsvRecord(4, 8, new[] { "3", "Carol" })
            });
            var conversions = 0;
            var importer = new CsvImporter<Customer>(new FakeReader(raw), values => {
                conversions++;
                return MapCustomer(values);
            });
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            }, new[] { new FieldPatternRule("Name", "^[A-Z][a-z]+$") });

            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "pattern-fake");
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(conversions, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Carol"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(5));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-invalid"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-031")]
        [Property("Scenario", "FR-031-SC01")]
        public void FR_031_SC01_FailFastStopsBeforeMappingLaterRowsAndKeepsFirstFailure()
        {
            var mapped = 0;
            var importer = new CsvImporter<Customer>(
                new FakeReader(CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Id", "Name" }),
                    new CsvRecord(2, 2, new[] { "1", "Alice" }),
                    new CsvRecord(3, 4, new[] { "2", "private-bad" }),
                    new CsvRecord(4, 6, new[] { "3", "Carol" })
                })),
                values => {
                    mapped++;
                    return MapCustomer(values);
                });
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true, null, 5)
            });
            using (var input = new StringReader("ignored"))
            {
                var result = importer.Import(input, 4096, schema, CsvHeaderPolicy.WithHeader,
                    "stop-source", null, null, true);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Stopped));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(mapped, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(4));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-bad"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-031")]
        [Property("Scenario", "FR-031-SC02")]
        public void FR_031_SC02_FailFastAcceptsAllValidRowsWhenNoErrors()
        {
            var importer = new CsvImporter<Customer>(
                new FakeReader(CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Id", "Name" }),
                    new CsvRecord(2, 2, new[] { "1", "Alice" }),
                    new CsvRecord(3, 3, new[] { "2", "Bob" })
                })), MapCustomer);
            using (var input = new StringReader("ignored"))
            {
                var result = importer.Import(input, 4096, Schema(),
                    CsvHeaderPolicy.WithHeader, "all-valid", null, null, true);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Diagnostics, Is.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_CancellationDuringMappingStopsBeforeNextRowWithoutLosingPrefix()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var mapped = 0;
                var importer = new CsvImporter<Customer>(
                    new FakeReader(CsvParseResult.Success(new[] {
                        new CsvRecord(1, 1, new[] { "Id", "Name" }),
                        new CsvRecord(2, 2, new[] { "1", "Alice" }),
                        new CsvRecord(3, 4, new[] { "2", "Bob" }),
                        new CsvRecord(4, 6, new[] { "3", "Carol" })
                    })),
                    values => {
                        mapped++;
                        var customer = MapCustomer(values);
                        if (mapped == 1) cancellation.Cancel();
                        return customer;
                    });
                using (var source = new StringReader("unused"))
                {
                    var result = importer.Import(source, 4096, Schema(),
                        CsvHeaderPolicy.WithHeader, "fr032-fake", null, null, false,
                        cancellation.Token);
                    Assert.That(result.Status, Is.EqualTo(ImportStatus.Canceled));
                    Assert.That(result.Summary.ProcessedCount, Is.EqualTo(1));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                    Assert.That(result.Summary.RejectedCount, Is.Zero);
                    Assert.That(result.Diagnostics, Is.Empty);
                    Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                    Assert.That(mapped, Is.EqualTo(1));
                }
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC02")]
        public void FR_032_SC02_UncanceledTokenCompletesNormallyWithEveryValidRow()
        {
            var importer = new CsvImporter<Customer>(
                new FakeReader(CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Id", "Name" }),
                    new CsvRecord(2, 2, new[] { "1", "Alice" }),
                    new CsvRecord(3, 3, new[] { "2", "Bob" })
                })), MapCustomer);
            using (var cancellation = new CancellationTokenSource())
            using (var source = new StringReader("unused"))
            {
                var result = importer.Import(source, 4096, Schema(),
                    CsvHeaderPolicy.WithHeader, "fr032-valid", null, null, false,
                    cancellation.Token);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords, Has.Count.EqualTo(2));
                Assert.That(result.Diagnostics, Is.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_CancellationDuringConversionStopsLaterRowsWithCanceledStatus()
        {
            using (var cancel = new System.Threading.CancellationTokenSource())
            {
                var mapped = 0;
                var importer = new CsvImporter<Customer>(
                    new FakeReader(CsvParseResult.Success(new[] {
                        new CsvRecord(1, 1, new[] { "Id", "Name" }),
                        new CsvRecord(2, 2, new[] { "1", "Alice" }),
                        new CsvRecord(3, 3, new[] { "2", "Bob" }),
                        new CsvRecord(4, 4, new[] { "3", "Carol" })
                    })), values => {
                        mapped++;
                        var record = MapCustomer(values);
                        if (record.Id == 1)
                            cancel.Cancel();
                        return record;
                    });
                using (var source = new StringReader("unused"))
                {
                    var result = importer.Import(source, 4096, Schema(),
                        CsvHeaderPolicy.WithHeader, "cancel-source",
                        null, null, false, cancel.Token);
                    Assert.That(result.Status, Is.EqualTo(ImportStatus.Canceled));
                    Assert.That(result.Summary.ProcessedCount, Is.EqualTo(1));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                    Assert.That(result.Summary.RejectedCount, Is.Zero);
                    Assert.That(mapped, Is.EqualTo(1));
                    Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                    Assert.That(result.Diagnostics, Is.Empty);
                }
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC02")]
        public void FR_032_SC02_UncanceledTokenCompletesEveryValidRecord()
        {
            using (var cancel = new System.Threading.CancellationTokenSource())
            {
                var importer = new CsvImporter<Customer>(new FakeReader(
                    CsvParseResult.Success(new[] {
                        new CsvRecord(1, 1, new[] { "Id", "Name" }),
                        new CsvRecord(2, 2, new[] { "1", "Alice" }),
                        new CsvRecord(3, 3, new[] { "2", "Bob" })
                    })), MapCustomer);
                using (var source = new StringReader("unused"))
                {
                    var result = importer.Import(source, 4096, Schema(),
                        CsvHeaderPolicy.WithHeader, "no-cancel",
                        null, null, false, cancel.Token);
                    Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                    Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                    Assert.That(result.Diagnostics, Is.Empty);
                }
            }
        }
    }
}
