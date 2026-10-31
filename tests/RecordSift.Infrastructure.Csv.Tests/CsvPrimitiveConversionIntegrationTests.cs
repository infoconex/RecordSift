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
    public sealed class CsvPrimitiveConversionIntegrationTests
    {
        private sealed class Order
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime OrderDate { get; set; }
            public bool Active { get; set; }
            public int? Score { get; set; }
        }

        private static TypedImportResult<Order> Import(string csv)
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true), new ExpectedColumn("Amount", true),
                new ExpectedColumn("Order Date", true), new ExpectedColumn("Active", true),
                new ExpectedColumn("Score", false)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Order Date", "OrderDate"),
                new SourcePropertyMapping("Active", "Active"),
                new SourcePropertyMapping("Score", "Score")
            });
            var mapper = new PrimitivePropertyMapper<Order>(
                mapping, CultureInfo.GetCultureInfo("fr-FR"));
            var importer = new CsvImporter<Order>(new CsvRecordReaderAdapter(), mapper.Map);
            using (var input = new StringReader(csv))
                return importer.Import(input, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "orders");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-011")]
        [Property("Scenario", "FR-011-SC01")]
        [Property("Scenario", "FR-011-SC02")]
        public void FR_011_SC01_SC02_RealCsvRespectsExplicitFrenchCultureAndNullableValues()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "2,secret-value,01/01/2025,false,5");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.50m));
            Assert.That(result.AcceptedRecords[0].OrderDate,
                Is.EqualTo(new DateTime(2025, 12, 31)));
            Assert.That(result.AcceptedRecords[0].Active, Is.True);
            Assert.That(result.AcceptedRecords[0].Score, Is.Null);
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("secret-value"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-018")]
        [Property("Scenario", "FR-018-SC01")]
        public void FR_018_SC01_RealCsvRejectsInvalidDecimalDateOverflowAndLossyPrecision()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "2,not-a-number,01/01/2025,true,\r\n" +
                "3,\"2,50\",not-a-date,true,\r\n" +
                "4,\"7,25\",02/01/2025,false,4\r\n" +
                "2147483648,\"3,50\",01/01/2025,true,5\r\n" +
                "6,\"0,1234567890123456789012345678912345\",01/01/2025,true,6");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(6));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(4));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(4));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Order Date"));
            Assert.That(result.Diagnostics[2].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[3].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(3));
            Assert.That(result.Diagnostics[2].Row, Is.EqualTo(5));
            Assert.That(result.Diagnostics[3].Row, Is.EqualTo(6));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].PhysicalLine, Is.EqualTo(4));
            Assert.That(result.Diagnostics[2].PhysicalLine, Is.EqualTo(6));
            Assert.That(result.Diagnostics[3].PhysicalLine, Is.EqualTo(7));
            foreach (var diagnostic in result.Diagnostics)
            {
                Assert.That(diagnostic.Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(diagnostic.Message, Does.Not.Contain("not-a-number"));
                Assert.That(diagnostic.Message, Does.Not.Contain("not-a-date"));
                Assert.That(diagnostic.Message,
                    Does.Not.Contain("0,1234567890123456789012345678912345"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-018")]
        [Property("Scenario", "FR-018-SC02")]
        public void FR_018_SC02_RealCsvPreservesValidValuesUnderConfiguredCulture()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "2,\"-7,25\",01/01/2026,false,9\r\n" +
                "3,\"0,1234567890123456789012345678\",02/01/2026,true,4");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.50m));
            Assert.That(result.AcceptedRecords[0].OrderDate,
                Is.EqualTo(new DateTime(2025, 12, 31)));
            Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(-7.25m));
            Assert.That(result.AcceptedRecords[1].OrderDate,
                Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(result.AcceptedRecords[1].Score, Is.EqualTo(9));
            Assert.That(result.AcceptedRecords[2].Amount,
                Is.EqualTo(0.1234567890123456789012345678m));
            Assert.That(result.AcceptedRecords[2].OrderDate,
                Is.EqualTo(new DateTime(2026, 1, 2)));
        }
    }
}
