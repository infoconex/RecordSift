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
using RecordSift.Infrastructure.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    /// <summary>
    /// Compiles and executes the documented consumer workflow in a separate
    /// assembly using only intentional public contracts and a real CSV adapter.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class PublicCsvApiAcceptanceTests
    {
        public sealed class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private static ImportSchema CreateSchema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true)
            });
        }

        private static CsvImporter<Customer> CreateImporter(ImportSchema schema)
        {
            var properties = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<Customer>(
                properties, CultureInfo.InvariantCulture);
            return new CsvImporter<Customer>(new CsvRecordReaderAdapter(), mapper.Map);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-001")]
        [Property("Scenario", "NFR-001-SC01")]
        public void NFR_001_SC01_PublicCsvConsumerCompilesImportsAndInspectsRecordsAndDiagnostics()
        {
            var schema = CreateSchema();
            var importer = CreateImporter(schema);

            using (var source = new StringReader("Id,Name\n1,Alice\ninvalid,Bob"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "customers", options: CsvReadOptions.Default);

                Assert.That(CsvReadOptions.Default.Delimiter, Is.EqualTo(","));
                Assert.That(result.SourceId, Is.EqualTo("customers"));
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords, Has.Count.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-001")]
        [Property("Scenario", "NFR-001-SC02")]
        public void NFR_001_SC02_PublicCsvRejectsConflictingHeaderAndPositionConfiguration()
        {
            var schema = CreateSchema();
            var importer = CreateImporter(schema);
            using (var source = new StringReader("Id,Name\n1,Alice"))
            {
                var error = Assert.Throws<ArgumentException>(() => importer.Import(
                    source, 4096, schema, CsvHeaderPolicy.WithHeader, "customers",
                    positions: new[] { new SourceColumnPosition("Id", 0) }));

                Assert.That(error.ParamName, Is.EqualTo("positions"));
                Assert.That(error.Message, Is.Not.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-001")]
        [Property("Scenario", "NFR-001-SC02")]
        public void NFR_001_SC02_PublicCsvRejectsMissingHeaderlessMappingForNonemptyInput()
        {
            var schema = CreateSchema();
            var importer = CreateImporter(schema);
            using (var source = new StringReader("1,Alice"))
            {
                var error = Assert.Throws<ArgumentException>(() => importer.Import(
                    source, 4096, schema, CsvHeaderPolicy.WithoutHeader, "customers"));

                Assert.That(error.ParamName, Is.EqualTo("positions"));
                Assert.That(error.Message, Is.Not.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-001")]
        [Property("Scenario", "NFR-001-SC02")]
        public void NFR_001_SC02_PublicCsvRejectsInvalidDelimiterWithExplicitSourceDiagnostic()
        {
            var schema = CreateSchema();
            var importer = CreateImporter(schema);
            using (var source = new StringReader("Id,Name\n1,Alice"))
            {
                var result = importer.Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "customers",
                    options: new CsvReadOptions("||"));

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.InvalidCsvDelimiter));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-002")]
        [Property("Scenario", "NFR-002-SC01")]
        [Property("Scenario", "NFR-002-SC02")]
        public void NFR_002_SC01_SC02_LocalCsvImportCompletesWithNoServiceConfiguration()
        {
            var schema = CreateSchema();
            using (var source = new StringReader("Id,Name\n1,Alice\ninvalid,Bob"))
            {
                var result = CreateImporter(schema).Import(source, 4096, schema,
                    CsvHeaderPolicy.WithHeader, "offline-csv", options: CsvReadOptions.Default);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            }
        }
    }
}
