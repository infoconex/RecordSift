using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Messages;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvImporterIntegrationTests
    {
        private sealed class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Id", true), new ExpectedColumn("Name", true)
            });
        }

        private static TypedImportResult<Customer> Import(string csv,
            CsvHeaderPolicy policy = CsvHeaderPolicy.WithHeader)
        {
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                values => new Customer {
                    Id = int.Parse(values["Id"], NumberStyles.Integer,
                        CultureInfo.InvariantCulture),
                    Name = values["Name"]
                });
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, Schema(), policy, "fixture-01");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC01")]
        [Property("Scenario", "FR-001-SC04")]
        public void FR_001_SC01_SC04_RealCsvImportReturnsThreeTypedOrderedQuotedCustomers()
        {
            var result = Import("Id,Name\r\n1,\"Alice, Jr.\"\r\n2,\"Bob\r\nTwo\"\r\n3,Carol");
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice, Jr."));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob\r\nTwo"));
            Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo(3));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC02")]
        [Property("Scenario", "FR-001-SC05")]
        [Property("Scenario", "FR-001-SC06")]
        public void FR_001_SC02_SC05_SC06_RealCsvImportDistinguishesEmptyOutcomes()
        {
            var headed = Import("Id,Name\r\n");
            var headerless = Import("", CsvHeaderPolicy.WithoutHeader);
            var missing = Import("");

            Assert.That(headed.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headed.Summary.ProcessedCount, Is.Zero);
            Assert.That(headed.Diagnostics, Is.Empty);
            Assert.That(headed.Message, Does.Contain("No data records"));
            Assert.That(headerless.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headerless.Diagnostics, Is.Empty);
            Assert.That(headerless.Message, Does.Contain("no records").IgnoreCase);
            Assert.That(missing.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(missing.Summary.ProcessedCount, Is.Zero);
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC03")]
        public void FR_001_SC03_RealMalformedCsvFailsAtomicallyWithLocation()
        {
            var result = Import("Id,Name\r\n1,Alice\r\n2,\"unfinished");
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
        }

        [Test]
        public void RealCsvConversionErrorRejectsOneRecordWithoutLeakingSourceValues()
        {
            var result = Import("Id,Name\nnot-an-id,Secret Customer\n2,Bob");
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Bob"));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("Secret Customer"));
        }

        private static TypedImportResult<Customer> ImportWithDeclaredMapping(string csv,
            CsvHeaderPolicy policy = CsvHeaderPolicy.WithHeader,
            IEnumerable<SourceColumnPosition> positions = null)
        {
            var schema = Schema();
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<Customer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<Customer>(
                new CsvRecordReaderAdapter(), mapper.Map);
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema, policy, "fr001-mapped",
                    positions);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC01")]
        public void FR_001_SC01_RealCsvUsesDeclaredMappingForThreeTypedCustomers()
        {
            var result = ImportWithDeclaredMapping(
                "Name,Id\r\nAlice,1\r\nBob,2\r\nCarol,3");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.SourceId, Is.EqualTo("fr001-mapped"));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(3));
            Assert.That(result.AcceptedRecords[0], Is.TypeOf<Customer>());
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
            Assert.That(result.AcceptedRecords[2].Id, Is.EqualTo(3));
            Assert.That(result.AcceptedRecords[2].Name, Is.EqualTo("Carol"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC02")]
        public void FR_001_SC02_RealCsvMappedHeaderOnlyCompletesWithInformation()
        {
            var result = ImportWithDeclaredMapping("Id,Name\r\n");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Message, Is.EqualTo(CsvMessageResources.ImportNoDataRecords));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC03")]
        public void FR_001_SC03_RealCsvMappedMalformedQuoteFailsWithoutPartialRecords()
        {
            var result = ImportWithDeclaredMapping(
                "Id,Name\r\n1,Alice\r\n2,\"unfinished");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.SourceId, Is.EqualTo("fr001-mapped"));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("unfinished"));
            Assert.That(result.Message, Is.EqualTo(CsvMessageResources.ImportFailed));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC04")]
        public void FR_001_SC04_RealCsvMappedQuotedDelimiterAndNewlineRemainFieldValues()
        {
            var result = ImportWithDeclaredMapping(
                "Id,Name\r\n1,\"Alice, Jr.\"\r\n2,\"Bob\r\nTwo\"");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice, Jr."));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob\r\nTwo"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC05")]
        public void FR_001_SC05_RealCsvMappedEmptyHeaderlessCompletesWithoutErrors()
        {
            var result = ImportWithDeclaredMapping("", CsvHeaderPolicy.WithoutHeader);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Message, Is.EqualTo(CsvMessageResources.ImportEmptySource));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC06")]
        public void FR_001_SC06_RealCsvMappedEmptyHeadedSourceFailsWithSchemaDiagnostic()
        {
            var result = ImportWithDeclaredMapping("");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Message, Is.EqualTo(CsvMessageResources.ImportMissingHeader));
        }
        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-013")]
        [Property("Scenario", "FR-013-SC01")]
        public void FR_013_SC01_RealCsvNamedAndPositionalBindingsReturnEquivalentTypedRecords()
        {
            var named = ImportWithDeclaredMapping(
                "Id,Name\r\n1,Alice\r\n2,Bob");
            var positional = ImportWithDeclaredMapping(
                "Alice,1\r\nBob,2",
                CsvHeaderPolicy.WithoutHeader, new[] {
                    new SourceColumnPosition("Name", 0),
                    new SourceColumnPosition("Id", 1)
                });

            Assert.That(named.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(positional.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(named.Diagnostics, Is.Empty);
            Assert.That(positional.Diagnostics, Is.Empty);
            Assert.That(named.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(positional.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(named.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(positional.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(named.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(positional.AcceptedRecords[0].Id, Is.EqualTo(named.AcceptedRecords[0].Id));
            Assert.That(positional.AcceptedRecords[0].Name, Is.EqualTo(named.AcceptedRecords[0].Name));
            Assert.That(named.AcceptedRecords[1].Id, Is.EqualTo(2));
            Assert.That(positional.AcceptedRecords[1].Id, Is.EqualTo(named.AcceptedRecords[1].Id));
            Assert.That(positional.AcceptedRecords[1].Name, Is.EqualTo(named.AcceptedRecords[1].Name));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-013")]
        [Property("Scenario", "FR-013-SC02")]
        public void FR_013_SC02_RealCsvRejectsOutOfRangePositionAsSourceError()
        {
            var result = ImportWithDeclaredMapping(
                "Alice,1\r\nBob,2", CsvHeaderPolicy.WithoutHeader,
                new[] {
                    new SourceColumnPosition("Name", 0),
                    new SourceColumnPosition("Id", 5)
                });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.SourceId, Is.EqualTo("fr001-mapped"));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.InvalidColumnPosition));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("Alice"));
        }

    }
}
