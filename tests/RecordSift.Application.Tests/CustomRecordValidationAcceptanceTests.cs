using System;
using System.Collections.Generic;
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
    [Category("Unit")]
    public sealed class CustomRecordValidationAcceptanceTests
    {
        private sealed class Customer
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private sealed class Reader : ICsvRecordReader
        {
            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                return CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Id", "Name" }),
                    new CsvRecord(2, 2, new[] { "1", "Alice" }),
                    new CsvRecord(3, 4, new[] { "2", "private" }),
                    new CsvRecord(4, 6, new[] { "3", "Carol" })
                });
            }
        }

        private static TypedImportResult<Customer> Import(
            IEnumerable<RecordValidationRule<Customer>> rules)
        {
            var importer = new CsvImporter<Customer>(new Reader(),
                values => new Customer { Id = values["Id"], Name = values["Name"] },
                null, null, rules);
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            });
            using (var source = new StringReader("ignored"))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "fr022-source");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-022")]
        [Property("Scenario", "FR-022-SC01")]
        public void FR_022_SC01_IndependentCustomRulesRejectOneRowWithOrderedSanitizedDiagnostics()
        {
            var result = Import(new[] {
                new RecordValidationRule<Customer>("Name", x => x.Name != "private"),
                new RecordValidationRule<Customer>(null, x => x.Id != "2")
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[1].Field, Is.Null);
            foreach (var error in result.Diagnostics)
            {
                Assert.That(error.Row, Is.EqualTo(2));
                Assert.That(error.PhysicalLine, Is.EqualTo(4));
                Assert.That(error.Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
                Assert.That(error.Message, Does.Not.Contain("private"));
            }
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Carol"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-022")]
        [Property("Scenario", "FR-022-SC02")]
        public void FR_022_SC02_PassingCustomRulesAcceptRecordsInSourceOrder()
        {
            var result = Import(new[] {
                new RecordValidationRule<Customer>("Name", x => x.Name.Length > 0),
                new RecordValidationRule<Customer>(null, x => x.Id != null)
            });
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo("1"));
            Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo("3"));
        }
    }
}
