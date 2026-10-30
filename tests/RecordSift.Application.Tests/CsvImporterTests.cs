using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Tests
{
    [TestFixture]
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
    }
}
