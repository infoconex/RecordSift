using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Xlsx;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Xlsx;

namespace RecordSift.Infrastructure.Xlsx.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class XlsxWorksheetSelectionAcceptanceTests
    {
        private sealed class Customer
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        // Actual two-worksheet OpenXML workbook; the first contains unrelated data.
        private const string Workbook = "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";

        private static XlsxImporter<Customer> Importer()
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
        [Property("Requirement", "FR-005")]
        [Property("Scenario", "FR-005-SC01")]
        public void FR_005_SC01_ImportsOnlyNamedWorksheetAndPreservesDiagnosticProvenance()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(Workbook)))
            {
                var result = Importer().Import(stream, 4096, Schema(), "selected",
                    worksheetName: "Customers");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Bob"));
                Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(result.Diagnostics[0].Worksheet, Is.EqualTo("Customers"));
                Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].PhysicalRow, Is.EqualTo(3));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-005")]
        [Property("Scenario", "FR-005-SC02")]
        public void FR_005_SC02_MissingWorksheetFailsWithoutFallback()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(Workbook)))
            {
                var result = Importer().Import(stream, 4096, Schema(), "missing",
                    worksheetName: "DoesNotExist");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.WorksheetNotFound));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("DoesNotExist"));
            }
        }

        [Test]
        [Property("Requirement", "FR-005")]
        public void WorksheetSelectionUsesExactOrdinalCaseSensitiveNames()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(Workbook)))
            {
                var result = Importer().Import(stream, 4096, Schema(), "case-sensitive",
                    worksheetName: "customers");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.WorksheetNotFound));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Requirement", "FR-005")]
        public void BlankWorksheetSelectionIsRejectedAsInvalidCallerArgument()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(Workbook)))
            {
                Assert.Throws<ArgumentException>(() =>
                    Importer().Import(stream, 4096, Schema(), "blank",
                        worksheetName: " "));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Requirement", "FR-005")]
        public void DefaultSelectionContinuesToReadFirstWorksheet()
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(Workbook)))
            {
                var result = Importer().Import(stream, 4096, Schema(), "default");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Eve"));
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(999));
            }
        }
    }
}
