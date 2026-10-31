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
    }
}
