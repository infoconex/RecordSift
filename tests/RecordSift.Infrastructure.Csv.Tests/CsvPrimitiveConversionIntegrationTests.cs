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


        private sealed class RestrictiveCustomer
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


        private sealed class FailingTextReader : TextReader
        {
            private readonly string _availableInput;
            private int _position;

            public FailingTextReader(string availableInput)
            {
                _availableInput = availableInput;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (_position >= _availableInput.Length)
                    throw new IOException("private-reader-failure");

                var available = Math.Min(count, _availableInput.Length - _position);
                _availableInput.CopyTo(_position, buffer, index, available);
                _position += available;
                return available;
            }

            public override int Read()
            {
                if (_position >= _availableInput.Length)
                    throw new IOException("private-reader-failure");
                return _availableInput[_position++];
            }
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

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-026")]
        [Property("Scenario", "FR-026-SC01")]
        public void FR_026_SC01_RealCsvMaterializesDeclaredTypedOrders()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "2,\"7,25\",01/01/2026,false,8");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0], Is.TypeOf<Order>());
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.50m));
            Assert.That(result.AcceptedRecords[0].OrderDate,
                Is.EqualTo(new DateTime(2025, 12, 31)));
            Assert.That(result.AcceptedRecords[0].Active, Is.True);
            Assert.That(result.AcceptedRecords[0].Score, Is.Null);
            Assert.That(result.AcceptedRecords[1], Is.TypeOf<Order>());
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(7.25m));
            Assert.That(result.AcceptedRecords[1].OrderDate,
                Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(result.AcceptedRecords[1].Active, Is.False);
            Assert.That(result.AcceptedRecords[1].Score, Is.EqualTo(8));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-026")]
        [Property("Scenario", "FR-026-SC02")]
        public void FR_026_SC02_RealCsvClassifiesSetterRejectionAndKeepsValidTypedRows()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Name", true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Customer Id", "CustomerId"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<RestrictiveCustomer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<RestrictiveCustomer>(
                new CsvRecordReaderAdapter(), mapper.Map);

            using (var source = new StringReader(
                "Customer Id,Name\r\n1,Alice\r\n2,reject-me\r\n3,Bob"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "customers-integration");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.SourceId, Is.EqualTo("customers-integration"));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].CustomerId, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[1].CustomerId, Is.EqualTo(3));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
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
        [Property("Requirement", "FR-027")]
        [Property("Scenario", "FR-027-SC01")]
        public void FR_027_SC01_RealCsvPreservesAcceptedOrderAcrossMultipleRejectedRecords()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "invalid-id,\"1,50\",31/12/2025,true,\r\n" +
                "3,\"7,25\",01/01/2026,false,8\r\n" +
                "4,invalid-amount,01/01/2026,true,\r\n" +
                "5,\"5,50\",02/01/2026,true,2");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(5));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(3));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(3));
            Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo(5));
            Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(7.25m));
            Assert.That(result.AcceptedRecords[2].Score, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(4));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[1].PhysicalLine, Is.EqualTo(5));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("invalid-id"));
            Assert.That(result.Diagnostics[1].Message, Does.Not.Contain("invalid-amount"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-027")]
        [Property("Scenario", "FR-027-SC02")]
        public void FR_027_SC02_RealCsvReportsDiagnosticsWithoutAnyAcceptedRecords()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "invalid-id,\"1,50\",31/12/2025,true,\r\n" +
                "2,invalid-amount,01/01/2026,false,\r\n" +
                "3,\"3,00\",not-a-date,true,8");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(3));
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(3));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[2].Row, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[2].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("invalid-id"));
            Assert.That(result.Diagnostics[1].Message, Does.Not.Contain("invalid-amount"));
            Assert.That(result.Diagnostics[2].Message, Does.Not.Contain("not-a-date"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-029")]
        [Property("Scenario", "FR-029-SC01")]
        public void FR_029_SC01_RealCsvRecoversFromInvalidRecordAndAcceptsLaterRow()
        {
            var result = Import(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n" +
                "2,private-invalid-amount,01/01/2026,true,\r\n" +
                "3,\"7,25\",02/01/2026,false,8");

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
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-invalid-amount"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-029")]
        [Property("Scenario", "FR-029-SC02")]
        public void FR_029_SC02_RealCsvReportsLateReaderFailureAsFatalSourceError()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Amount", true),
                new ExpectedColumn("Order Date", true),
                new ExpectedColumn("Active", true),
                new ExpectedColumn("Score", false)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Order Date", "OrderDate"),
                new SourcePropertyMapping("Active", "Active"),
                new SourcePropertyMapping("Score", "Score")
            });
            var mapper = new PrimitivePropertyMapper<Order>(mapping,
                CultureInfo.GetCultureInfo("fr-FR"));
            var importer = new CsvImporter<Order>(new CsvRecordReaderAdapter(), mapper.Map);

            // The first batch contains a complete valid record. The reader then
            // fails on a subsequent read rather than producing a reliable EOF.
            using (var source = new FailingTextReader(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,\"12,50\",31/12/2025,true,\r\n"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "broken-source");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.SourceId, Is.EqualTo("broken-source"));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Summary.AcceptedCount, Is.Zero);
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceUnreadable));
                Assert.That(result.Diagnostics[0].Row, Is.Null);
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-reader-failure"));
                Assert.That(result.Message, Is.Not.Empty);
            }
        }
        private static TypedImportResult<Order> ImportRequiredOrders(string csv)
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Amount", true, true),
                new ExpectedColumn("Order Date", true),
                new ExpectedColumn("Active", true),
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
            var importer = new CsvImporter<Order>(
                new CsvRecordReaderAdapter(), mapper.Map);
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "diagnostic-orders");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-028")]
        [Property("Scenario", "FR-028-SC01")]
        public void FR_028_SC01_RealCsvDiagnosticsIdentifySourceRowAndInvalidField()
        {
            var result = ImportRequiredOrders(
                "Id,Amount,Order Date,Active,Score\r\n" +
                "1,   ,31/12/2025,true,\r\n" +
                "2,\"5,50\",01/01/2026,true,");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.SourceId, Is.EqualTo("diagnostic-orders"));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-028")]
        [Property("Scenario", "FR-028-SC02")]
        public void FR_028_SC02_RealCsvTwoIndependentErrorsRejectOneRecordOnce()
        {
            var result = ImportRequiredOrders(
                "Id,Amount,Order Date,Active,Score\r\n" +
                ",   ,31/12/2025,true,\r\n" +
                "2,\"5,50\",01/01/2026,true,");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[1].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(result.Diagnostics[1].Code,
                Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].PhysicalLine, Is.EqualTo(2));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-028")]
        [Property("Scenario", "FR-028-SC03")]
        public void FR_028_SC03_RealCsvMissingHeaderFailsWithoutRejectedRow()
        {
            var result = ImportRequiredOrders("");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.SourceId, Is.EqualTo("diagnostic-orders"));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Message, Is.Not.Empty);
        }

    }
}
