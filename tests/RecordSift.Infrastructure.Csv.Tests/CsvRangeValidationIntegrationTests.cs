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
    public sealed class CsvRangeValidationIntegrationTests
    {
        private sealed class Invoice
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime DueDate { get; set; }
        }

        private static TypedImportResult<Invoice> Import(string csv)
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Amount", true, true),
                new ExpectedColumn("DueDate", true, true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("DueDate", "DueDate")
            });
            var mapper = new PrimitivePropertyMapper<Invoice>(
                mapping, CultureInfo.InvariantCulture);
            var rules = new[] {
                FieldRangeRule<Invoice>.Numeric("Amount", x => x.Amount,
                    0m, true, 100m, true),
                FieldRangeRule<Invoice>.Date("DueDate", x => x.DueDate,
                    new DateTime(2024, 1, 1), true,
                    new DateTime(2024, 12, 31), true)
            };
            var importer = new CsvImporter<Invoice>(
                new CsvRecordReaderAdapter(), mapper.Map, rules);

            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "fr020-invoices");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-020")]
        [Property("Scenario", "FR-020-SC01")]
        public void FR_020_SC01_OutOfRangeNumericAndDateFieldsProduceDistinctRecordDiagnostics()
        {
            var result = Import(
                "Id,Amount,DueDate\r\n1,200.00,2023-12-31\r\n2,42.00,2024-06-01");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.SourceId, Is.EqualTo("fr020-invoices"));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.FieldRangeOutOfRange));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].Code,
                Is.EqualTo(DiagnosticCodes.FieldRangeOutOfRange));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("DueDate"));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("200.00"));
            Assert.That(result.Diagnostics[1].Message, Does.Not.Contain("2023-12-31"));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(42m));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-020")]
        [Property("Scenario", "FR-020-SC02")]
        public void FR_020_SC02_InclusiveNumericAndDateBoundsAcceptExactEndpoints()
        {
            var result = Import(
                "Id,Amount,DueDate\r\n1,0,2024-01-01\r\n2,100,2024-12-31");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(0m));
            Assert.That(result.AcceptedRecords[0].DueDate,
                Is.EqualTo(new DateTime(2024, 1, 1)));
            Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(100m));
            Assert.That(result.AcceptedRecords[1].DueDate,
                Is.EqualTo(new DateTime(2024, 12, 31)));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-020")]
        [Property("Scenario", "FR-020-SC01")]
        public void FR_020_SC01_UnconvertibleNumericValueIsNotMisreportedAsRangeFailure()
        {
            var result = Import("Id,Amount,DueDate\r\n1,private-invalid,2024-02-01");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(1));
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Message,
                Does.Not.Contain("private-invalid"));
        }
    }
}
