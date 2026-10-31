using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Application.Mapping;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Xlsx;

namespace RecordSift.Infrastructure.Xlsx.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class XlsxImporterAcceptanceTests
    {
        private sealed class Customer
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        // An actual, self-contained OpenXML workbook with sparse physical rows 1,3,4,6.
        // The fixture is base64 to retain binary fidelity through GitHub's text-only API.
        private const string ValidWorkbook = "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";

        private static XlsxImporter<Customer> CreateImporter()
        {
            return new XlsxImporter<Customer>(new XlsxWorksheetReaderAdapter(),
                values => new Customer {
                    Name = values["Name"],
                    Id = int.Parse(values["Id"], CultureInfo.InvariantCulture)
                });
        }

        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Name", true, true),
                new ExpectedColumn("Id", true, true)
            });
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-002")]
        [Property("Scenario", "FR-002-SC01")]
        public void FR_002_SC01_ImportsOrderedTypedWorksheetRowsWithPhysicalProvenance()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var result = CreateImporter().Import(stream, 4096, Schema(), "customers");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Worksheet, Is.EqualTo("Customers"));
                Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(4));
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.Null);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-002")]
        [Property("Scenario", "FR-002-SC02")]
        public void FR_002_SC02_InvalidXlsxReturnsAtomicSourceFailure()
        {
            using (var stream = new MemoryStream(new byte[] { 0, 4, 255, 6 }))
            {
                var result = CreateImporter().Import(stream, 4096, Schema(), "unsafe-source");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("unsafe-source"));
            }
        }

        [Test]
        [Property("Requirement", "FR-002")]
        public void ExceedingConfiguredWorkbookByteBudgetFailsWithoutPartialResults()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var result = CreateImporter().Import(stream, 64, Schema(), "bounded");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceLimitExceeded));
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Requirement", "FR-002")]
        public void ExceedingConfiguredPhysicalRowBudgetFailsAtomically()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var result = CreateImporter().Import(stream, 4096, Schema(), "bounded",
                    new RecordSift.Application.Ports.Xlsx.XlsxReadLimits(3, 16, 1024));
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceLimitExceeded));
                Assert.That(result.AcceptedRecords, Is.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-001")]
        [Property("Scenario", "NFR-001-SC01")]
        public void NFR_001_SC01_PublicXlsxConsumerImportsTypedRecordsUsingDocumentedDefaults()
        {
            var schema = Schema();
            var properties = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Name", "Name"),
                new SourcePropertyMapping("Id", "Id")
            });
            var mapper = new PrimitivePropertyMapper<Customer>(
                properties, CultureInfo.InvariantCulture);
            var importer = new XlsxImporter<Customer>(
                new XlsxWorksheetReaderAdapter(), mapper.Map);

            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var defaults = XlsxReadLimits.Default;
                Assert.That(defaults.MaxRows, Is.GreaterThan(0));
                Assert.That(defaults.MaxColumns, Is.GreaterThan(0));
                Assert.That(defaults.MaxCellCharacters, Is.GreaterThan(0));

                var result = importer.Import(stream, 4096, schema, "customers",
                    limits: defaults);

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Worksheet, Is.EqualTo("Customers"));
                Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(4));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-001")]
        [Property("Scenario", "NFR-001-SC02")]
        public void NFR_001_SC02_PublicXlsxConsumerRejectsBlankWorksheetAndInvalidLimits()
        {
            var importer = CreateImporter();
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var worksheetError = Assert.Throws<ArgumentException>(() =>
                    importer.Import(stream, 4096, Schema(), "customers",
                        worksheetName: "  "));
                Assert.That(worksheetError.ParamName, Is.EqualTo("worksheetName"));
                Assert.That(worksheetError.Message, Is.Not.Empty);

                var sizeError = Assert.Throws<ArgumentOutOfRangeException>(() =>
                    importer.Import(stream, 0, Schema(), "customers"));
                Assert.That(sizeError.ParamName, Is.EqualTo("maxBytes"));

                var limitError = Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new XlsxReadLimits(0, 1, 1));
                Assert.That(limitError.ParamName, Is.EqualTo("maxRows"));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-002")]
        [Property("Scenario", "NFR-002-SC01")]
        [Property("Scenario", "NFR-002-SC02")]
        public void NFR_002_SC01_SC02_LocalXlsxImportCompletesWithNoServiceConfiguration()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var result = CreateImporter().Import(stream, 4096, Schema(), "offline-xlsx");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-003")]
        [Property("Scenario", "NFR-003-SC01")]
        public void NFR_003_SC01_ImportsTypedXlsxOnRunnerWithoutExcelInstallation()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var result = CreateImporter().Import(stream, 4096, Schema(), "excel-free");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-003")]
        [Property("Scenario", "NFR-003-SC02")]
        public void NFR_003_SC02_ReaderUsesOpenXmlWithoutOfficeAutomation()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
            {
                var limits = XlsxReadLimits.Default;
                var result = new XlsxWorksheetReaderAdapter().Read(stream, 4096,
                    limits.MaxRows, limits.MaxColumns, limits.MaxCellCharacters);
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(result.Worksheet, Is.EqualTo("Customers"));
                Assert.That(result.Records, Has.Count.GreaterThan(1));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-004")]
        [Property("Scenario", "NFR-004-SC01")]
        public void NFR_004_SC01_RepeatedXlsxImportsPreserveOrderedRecordsAndDiagnostics()
        {
            for (var repetition = 0; repetition < 8; repetition++)
            {
                using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
                {
                    var result = CreateImporter().Import(stream, 4096, Schema(), "repeat-xlsx");
                    Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                    Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                    Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                    Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                    Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                    Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                    Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
                    Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                    Assert.That(result.Diagnostics[0].Code,
                        Is.EqualTo(DiagnosticCodes.ConversionFailed));
                    Assert.That(result.Diagnostics[0].Worksheet, Is.EqualTo("Customers"));
                    Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                    Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(4));
                }
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-004")]
        [Property("Scenario", "NFR-004-SC02")]
        public void NFR_004_SC02_XlsxExplicitMappingIgnoresAmbientCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var cultureName in new[] { "en-US", "fr-FR", "de-DE" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                    using (var stream = new MemoryStream(Convert.FromBase64String(ValidWorkbook)))
                    {
                        var result = CreateImporter().Import(stream, 4096, Schema(), "stable-xlsx");
                        Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                        Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                        Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                        Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                        Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                        Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                        Assert.That(result.Diagnostics[0].Code,
                            Is.EqualTo(DiagnosticCodes.ConversionFailed));
                        Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(4));
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        private sealed class CulturalOrder
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime OrderDate { get; set; }
        }

        // A local OpenXML workbook with inline-string French decimal and date cells.
        private const string CultureWorkbook = "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";

        private static TypedImportResult<CulturalOrder> ImportCultureWorkbook()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Amount", true, true),
                new ExpectedColumn("Order Date", true, true)
            });
            var properties = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Amount", "Amount"),
                new SourcePropertyMapping("Order Date", "OrderDate")
            });
            var mapper = new PrimitivePropertyMapper<CulturalOrder>(
                properties, CultureInfo.GetCultureInfo("fr-FR"));
            var importer = new XlsxImporter<CulturalOrder>(
                new XlsxWorksheetReaderAdapter(), mapper.Map);
            using (var stream = new MemoryStream(Convert.FromBase64String(CultureWorkbook)))
                return importer.Import(stream, 4096, schema, "locale-xlsx");
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-005")]
        [Property("Scenario", "NFR-005-SC01")]
        public void NFR_005_SC01_RealXlsxUsesExplicitFrenchDecimalAndDateConversion()
        {
            var result = ImportCultureWorkbook();
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.50m));
            Assert.That(result.AcceptedRecords[0].OrderDate,
                Is.EqualTo(new DateTime(2025, 12, 31)));
            Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(3));
            Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(7.25m));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(3));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-005")]
        [Property("Scenario", "NFR-005-SC02")]
        public void NFR_005_SC02_RealXlsxExplicitCultureRemainsStableAcrossAmbientChanges()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var name in new[] { "en-US", "de-DE", "fr-FR" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                    var result = ImportCultureWorkbook();
                    Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                    Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                    Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                    Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                    Assert.That(result.AcceptedRecords[0].Amount, Is.EqualTo(12.50m));
                    Assert.That(result.AcceptedRecords[0].OrderDate,
                        Is.EqualTo(new DateTime(2025, 12, 31)));
                    Assert.That(result.AcceptedRecords[1].Amount, Is.EqualTo(7.25m));
                    Assert.That(result.Diagnostics[0].Code,
                        Is.EqualTo(DiagnosticCodes.ConversionFailed));
                    Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                    Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(3));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "NFR-006")]
        [Property("Scenario", "NFR-006-SC01")]
        [Property("Scenario", "NFR-006-SC02")]
        public void NFR_006_SC01_SC02_CorruptXlsxReportsAtomicSanitizedSourceDiagnostic()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(
                "Not a ZIP package: /private/secret-workbook.xlsx");
            using (var source = new MemoryStream(bytes))
            {
                var result = CreateImporter().Import(source, 4096, Schema(), "corrupt-source");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Summary.AcceptedCount, Is.Zero);
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
                Assert.That(result.Diagnostics[0].Row, Is.Null);
                Assert.That(result.Diagnostics[0].PhysicalLine, Is.Null);
                Assert.That(result.Diagnostics[0].PhysicalRow, Is.Null);
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("secret-workbook"));
                Assert.That(result.Message, Does.Not.Contain("secret-workbook"));
                Assert.That(source.CanRead, Is.True);
            }
        }
    }
}
