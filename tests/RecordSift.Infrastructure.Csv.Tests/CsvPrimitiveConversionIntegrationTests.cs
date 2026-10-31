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

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-011")]
        [Property("Scenario", "FR-011-SC01")]
        [Property("Scenario", "FR-011-SC02")]
        public void FR_011_SC01_SC02_RealCsvRespectsExplicitFrenchCultureAndNullableValues()
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
            using (var input = new StringReader(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "2,secret-value,01/01/2025,false,5"))
            {
                var result = importer.Import(input, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "orders");
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
        }
    }
}
