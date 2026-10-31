using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Messages;
using RecordSift.Application.Ports.Csv;
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

        private sealed class FrenchInvoice
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
        }

        private sealed class OptionalCustomer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Note { get; set; }
        }

        private static TypedImportResult<OptionalCustomer> ImportOptionalCustomers(string csv)
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true),
                new ExpectedColumn("Note", false, false)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Name", "Name"),
                new SourcePropertyMapping("Note", "Note")
            });
            var mapper = new PrimitivePropertyMapper<OptionalCustomer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<OptionalCustomer>(
                new CsvRecordReaderAdapter(), mapper.Map);
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "fr012-optional");
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
            IEnumerable<SourceColumnPosition> positions = null,
            CsvReadOptions options = null)
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
                    positions, options);
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


        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-004")]
        [Property("Scenario", "FR-004-SC01")]
        public void FR_004_SC01_HeadedAndHeaderlessCsvProduceEquivalentOrderedTypedRecords()
        {
            var headed = ImportWithDeclaredMapping("Id,Name\r\n1,Alice\r\n2,Bob");
            var headerless = ImportWithDeclaredMapping("1,Alice\r\n2,Bob",
                CsvHeaderPolicy.WithoutHeader, new[] {
                    new SourceColumnPosition("Id", 0),
                    new SourceColumnPosition("Name", 1)
                });

            Assert.That(headed.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headerless.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headed.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(headerless.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(headed.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(headerless.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(headed.Summary.RejectedCount, Is.Zero);
            Assert.That(headerless.Summary.RejectedCount, Is.Zero);
            Assert.That(headed.Diagnostics, Is.Empty);
            Assert.That(headerless.Diagnostics, Is.Empty);
            Assert.That(headerless.AcceptedRecords, Has.Count.EqualTo(2));
            for (var index = 0; index < 2; index++)
            {
                Assert.That(headerless.AcceptedRecords[index], Is.TypeOf<Customer>());
                Assert.That(headerless.AcceptedRecords[index].Id,
                    Is.EqualTo(headed.AcceptedRecords[index].Id));
                Assert.That(headerless.AcceptedRecords[index].Name,
                    Is.EqualTo(headed.AcceptedRecords[index].Name));
            }
            Assert.That(headerless.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(headerless.AcceptedRecords[1].Id, Is.EqualTo(2));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-004")]
        [Property("Scenario", "FR-004-SC02")]
        public void FR_004_SC02_MissingDeclaredHeaderFailsWithoutAcceptingOrShiftingRows()
        {
            var result = ImportWithDeclaredMapping(
                "Id,Unexpected\r\n1,private-source-value");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredColumnMissing));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].Message,
                Does.Not.Contain("private-source-value"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-003")]
        [Property("Scenario", "FR-003-SC01")]
        public void FR_003_SC01_SemicolonCsvMapsQuotedValuesAndFrenchDecimalsToTypedRecords()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Description", true),
                new ExpectedColumn("Amount", true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Description", "Description"),
                new SourcePropertyMapping("Amount", "Amount")
            });
            var mapper = new PrimitivePropertyMapper<FrenchInvoice>(
                mapping, CultureInfo.GetCultureInfo("fr-FR"));
            var importer = new CsvImporter<FrenchInvoice>(
                new CsvRecordReaderAdapter(), mapper.Map);

            using (var source = new StringReader(
                "Id;Description;Amount\r\n1;\"Hello; World\";12,5\r\n2;Done;3,25"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "semicolon-invoices",
                    options: new CsvReadOptions(";"));

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(result.AcceptedRecords, Has.Count.EqualTo(2));
                Assert.That(result.AcceptedRecords[0], Is.TypeOf<FrenchInvoice>());
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Description, Is.EqualTo("Hello; World"));
                Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.5m));
                Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[1].Description, Is.EqualTo("Done"));
                Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(3.25m));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-003")]
        [Property("Scenario", "FR-003-SC02")]
        public void FR_003_SC02_InvalidDelimiterFailsTypedImportBeforeMalformedSourceIsParsed()
        {
            var result = ImportWithDeclaredMapping(
                "Id;Name\r\n1;\"private-unterminated",
                options: new CsvReadOptions(";;"));

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.InvalidCsvDelimiter));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].Message,
                Does.Not.Contain("private-unterminated"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-012")]
        [Property("Scenario", "FR-012-SC01")]
        public void FR_012_SC01_MissingRequiredAndOptionalValuesRejectOnlyTheInvalidRecord()
        {
            var result = ImportOptionalCustomers(
                "Id,Name,Note\r\n, ,\r\n2,Bob,\r\n3,Carol,observed");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.SourceId, Is.EqualTo("fr012-optional"));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].Code,
                Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[1].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[1].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Bob"));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(3));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Carol"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-012")]
        [Property("Scenario", "FR-012-SC02")]
        public void FR_012_SC02_AbsentOptionalSourceColumnLeavesDestinationUnchanged()
        {
            var result = ImportOptionalCustomers(
                "Id,Name\r\n1,Alice\r\n2,Bob");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(2));
            Assert.That(result.AcceptedRecords[0], Is.TypeOf<OptionalCustomer>());
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
            Assert.That(result.AcceptedRecords[0].Note, Is.Null);
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
            Assert.That(result.AcceptedRecords[1].Note, Is.Null);
        }

        private static TypedImportResult<Customer> ImportWithNameLengthConstraint(
            string csv, int? minimumLength, int? maximumLength)
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, false, minimumLength, maximumLength)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<Customer>(
                mapping, CultureInfo.InvariantCulture);
            var importer = new CsvImporter<Customer>(
                new CsvRecordReaderAdapter(), mapper.Map);
            using (var source = new StringReader(csv))
                return importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "fr019-length");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-019")]
        [Property("Scenario", "FR-019-SC01")]
        public void FR_019_SC01_OverlengthRealCsvValueRejectsOnlyItsRowWithFieldAndLocation()
        {
            var result = ImportWithNameLengthConstraint(
                "Id,Name\r\n1,private-long-value\r\n2,Bob", null, 4);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.SourceId, Is.EqualTo("fr019-length"));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Bob"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.FieldLengthOutOfRange));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Message,
                Does.Not.Contain("private-long-value"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-019")]
        [Property("Scenario", "FR-019-SC02")]
        public void FR_019_SC02_ExactInclusiveLengthBoundariesPreserveTypedCsvRecords()
        {
            var result = ImportWithNameLengthConstraint(
                "Id,Name\r\n1,AB\r\n2,WXYZ\r\n3, AB ", 2, 4);

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords, Has.Count.EqualTo(3));
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("AB"));
            Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("WXYZ"));
            Assert.That(result.AcceptedRecords[2].Name, Is.EqualTo(" AB "));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-006")]
        [Property("Scenario", "NFR-006-SC01")]
        [Property("Scenario", "NFR-006-SC02")]
        public void NFR_006_SC01_SC02_MalformedCsvAfterValidRowReportsSafeSourceLocationAtomically()
        {
            const string Sensitive = "private-token-2468";
            var result = Import("Id,Name\r\n1,Alice\r\n2,\"" + Sensitive + "\r\nunfinished");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.AcceptedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.AcceptedRecords, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Message, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain(Sensitive));
            Assert.That(result.Message, Does.Not.Contain(Sensitive));
        }

        private sealed class UnreadableCsvTextReader : TextReader
        {
            public override int Read(char[] buffer, int index, int count)
            {
                throw new IOException("Secret file: /private/customer-data.csv");
            }

            public override int Read()
            {
                throw new IOException("Secret file: /private/customer-data.csv");
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-006")]
        [Property("Scenario", "NFR-006-SC01")]
        [Property("Scenario", "NFR-006-SC02")]
        public void NFR_006_SC01_SC02_UnreadableCsvStreamReturnsSafeSourceFailure()
        {
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                values => new Customer { Name = values["Name"] });
            using (var source = new UnreadableCsvTextReader())
            {
                var result = importer.Import(source, 4096, Schema(),
                    CsvHeaderPolicy.WithHeader, "read-error");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Summary.AcceptedCount, Is.Zero);
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceUnreadable));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.Null);
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("/private/customer-data.csv"));
                Assert.That(result.Message, Does.Not.Contain("/private/customer-data.csv"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-007")]
        [Property("Scenario", "NFR-007-SC01")]
        public void NFR_007_SC01_CsvCharacterBudgetOverrunFailsAtomically()
        {
            const string Csv = "Id,Name\r\n1,Alice\r\n2,Bob";
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                values => new Customer { Id = int.Parse(values["Id"],
                    CultureInfo.InvariantCulture), Name = values["Name"] });
            using (var source = new StringReader(Csv))
            {
                var result = importer.Import(source, Csv.Length - 1, Schema(),
                    CsvHeaderPolicy.WithHeader, "bounded-csv");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Summary.AcceptedCount, Is.Zero);
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.SourceLimitExceeded));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-007")]
        [Property("Scenario", "NFR-007-SC02")]
        public void NFR_007_SC02_CsvExactAndMaximumIntegerBudgetsAllowValidInput()
        {
            const string Csv = "Id,Name\r\n1,Alice\r\n2,Bob";
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                values => new Customer { Id = int.Parse(values["Id"],
                    CultureInfo.InvariantCulture), Name = values["Name"] });
            foreach (var maxCharacters in new[] { Csv.Length, int.MaxValue })
            {
                using (var source = new StringReader(Csv))
                {
                    var result = importer.Import(source, maxCharacters, Schema(),
                        CsvHeaderPolicy.WithHeader, "bounded-csv");
                    Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                    Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                    Assert.That(result.Summary.RejectedCount, Is.Zero);
                    Assert.That(result.Diagnostics, Is.Empty);
                    Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                }
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-008")]
        [Property("Scenario", "NFR-008-SC02")]
        public void NFR_008_SC02_CsvExpressionsScriptsAndShellLikeStringsRemainLiteralData()
        {
            var result = Import("Id,Name\r\n1,=1+1\r\n2,<script>harmless()</script>\r\n3,cmd.exe /c echo marker");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("=1+1"));
            Assert.That(result.AcceptedRecords[1].Name,
                Is.EqualTo("<script>harmless()</script>"));
            Assert.That(result.AcceptedRecords[2].Name,
                Is.EqualTo("cmd.exe /c echo marker"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-031")]
        [Property("Scenario", "FR-031-SC01")]
        public void FR_031_SC01_RealCsvStopsAfterFirstConversionFailureAndRetainsPrefix()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            });
            var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                fields => new Customer {
                    Id = int.Parse(fields["Id"], System.Globalization.CultureInfo.InvariantCulture),
                    Name = fields["Name"]
                });
            using (var reader = new StringReader("Id,Name\r\n1,Alice\r\nnot-int,private\r\n3,Carol"))
            {
                var result = importer.Import(reader, 4096, schema, CsvHeaderPolicy.WithHeader,
                    "fr031-csv", null, null, true);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Stopped));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_RealCsvCancellationRetainsValidatedPrefixAndReaderOwnership()
        {
            using (var cancellation = new System.Threading.CancellationTokenSource())
            using (var source = new StringReader("Id,Name\r\n1,Alice\r\n2,Bob\r\n3,Carol"))
            {
                var mapped = 0;
                var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                    values => {
                        mapped++;
                        var result = new Customer {
                            Id = int.Parse(values["Id"], CultureInfo.InvariantCulture),
                            Name = values["Name"]
                        };
                        if (mapped == 1) cancellation.Cancel();
                        return result;
                    });
                var result = importer.Import(source, 4096, Schema(),
                    CsvHeaderPolicy.WithHeader, "fr032-real-csv",
                    null, null, false, cancellation.Token);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Canceled));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(1));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(mapped, Is.EqualTo(1));
                Assert.That(source.Peek(), Is.EqualTo(-1));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_PreCanceledRealCsvDoesNotReadSource()
        {
            using (var cancellation = new System.Threading.CancellationTokenSource())
            using (var source = new StringReader("Id,Name\r\n1,Alice"))
            {
                cancellation.Cancel();
                var importer = new CsvImporter<Customer>(new CsvRecordReaderAdapter(),
                    values => new Customer {
                        Id = int.Parse(values["Id"], CultureInfo.InvariantCulture),
                        Name = values["Name"]
                    });
                var result = importer.Import(source, 4096, Schema(),
                    CsvHeaderPolicy.WithHeader, "fr032-pre-canceled",
                    null, null, false, cancellation.Token);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Canceled));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(source.Peek(), Is.EqualTo((int)'I'));
            }
        }
    }
}
