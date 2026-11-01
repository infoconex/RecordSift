using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using RecordSift.Application.Ports.Reporting;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Infrastructure.Reporting;

namespace RecordSift.Infrastructure.Reporting.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvDiagnosticReportAcceptanceTests
    {
        private static List<string[]> ReadRows(string csv, string delimiter = ",")
        {
            var rows = new List<string[]>();
            using (var reader = new TextFieldParser(new StringReader(csv)))
            {
                reader.TextFieldType = FieldType.Delimited;
                reader.HasFieldsEnclosedInQuotes = true;
                // The default trims leading/trailing whitespace, hiding exact-value
                // regressions (including spreadsheet-sensitive tab/CR/LF prefixes).
                reader.TrimWhiteSpace = false;
                reader.SetDelimiters(delimiter);
                while (!reader.EndOfData)
                    rows.Add(reader.ReadFields());
            }
            return rows;
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-038")]
        [Property("Scenario", "FR-038-SC01")]
        public void FR_038_SC01_EscapesQuotedMultilineDiagnosticsAndPreservesOrderedProvenance()
        {
            const string PrivateValue = "secret-record-value-038";
            const string PrivateStatus = "secret caller status, do not export";
            const string MultilineMessage = "A value with a comma, \"quotes\" and\r\na second line";
            const string UnusualField = "Name,\"alias\"\nOther";
            var record = ImportDiagnostic.ForRecord(2, UnusualField,
                DiagnosticCodes.ValidationFailed, "Sheet,\"One\"", 11, 23);
            // The Domain normally supplies resource-backed fixed messages. A synthetic
            // message tests serializer escaping without weakening Domain invariants.
            typeof(ImportDiagnostic).GetProperty("Message")
                .GetSetMethod(true).Invoke(record, new object[] { MultilineMessage });
            var source = ImportDiagnostic.ForSource(null,
                DiagnosticCodes.MalformedSource, null, null, 29);
            var result = new TypedImportResult<string>("batch-038",
                ImportResult.Failed(2, new[] { record, source }),
                new[] { PrivateValue }, PrivateStatus);

            IImportResultSerializer serializer = new CsvImportResultSerializerAdapter();
            var csv = serializer.Serialize(result);
            var rows = ReadRows(csv);
            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[0], Is.EqualTo(new[] {
                "SourceId", "Scope", "Row", "Field", "Code", "Message",
                "Worksheet", "PhysicalRow", "PhysicalLine"
            }));
            Assert.That(rows[1], Is.EqualTo(new[] {
                "batch-038", "Record", "2", UnusualField,
                DiagnosticCodes.ValidationFailed, MultilineMessage, "Sheet,\"One\"", "11", "23"
            }));
            Assert.That(rows[2], Is.EqualTo(new[] {
                "batch-038", "Source", "", "", DiagnosticCodes.MalformedSource,
                source.Message, "", "", "29"
            }));
            Assert.That(csv, Does.Contain("\"\"quotes\"\""));
            Assert.That(csv, Does.Contain("\r\na second line"));
            Assert.That(csv, Does.Not.Contain(PrivateValue));
            Assert.That(csv, Does.Not.Contain(PrivateStatus));
            Assert.That(serializer.Serialize(result), Is.EqualTo(csv));
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-038")]
        [Property("Scenario", "FR-038-SC02")]
        public void FR_038_SC02_ErrorFreeCompletedResultProducesHeaderOnlyReport()
        {
            var result = new TypedImportResult<string>("clean-source",
                ImportResult.Completed(1, new ImportDiagnostic[0]),
                new[] { "private accepted value" }, "private caller message");
            var csv = new CsvImportResultSerializerAdapter().Serialize(result);
            Assert.That(csv, Is.EqualTo(
                "SourceId,Scope,Row,Field,Code,Message,Worksheet,PhysicalRow,PhysicalLine\r\n"));
            var rows = ReadRows(csv);
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(csv, Does.Not.Contain("private"));
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-038")]
        [Property("Scenario", "FR-038-SC02")]
        public void FR_038_SC02_CanceledWithoutDiagnosticsProducesHeaderOnlyReport()
        {
            var outcome = ImportResult.Canceled(0, new ImportDiagnostic[0]);
            var result = new TypedImportResult<string>("canceled-source",
                outcome, new string[0], "private cancellation message");
            var csv = new CsvImportResultSerializerAdapter().Serialize(result);
            Assert.That(ReadRows(csv), Has.Count.EqualTo(1));
            Assert.That(csv, Is.EqualTo(
                "SourceId,Scope,Row,Field,Code,Message,Worksheet,PhysicalRow,PhysicalLine\r\n"));
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Canceled));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        [Property("Requirement", "FR-038")]
        public void NonAsciiMetadataSurvivesCallerSelectedUtf8Encoding()
        {
            var diagnostic = ImportDiagnostic.ForRecord(1, "Région",
                DiagnosticCodes.ValidationFailed);
            var result = new TypedImportResult<string>("café-source",
                ImportResult.Completed(1, new[] { diagnostic }),
                new string[0], "ignored");
            var report = new CsvImportResultSerializerAdapter().Serialize(result);
            var bytes = new System.Text.UTF8Encoding(false, true).GetBytes(report);
            Assert.That(bytes[0], Is.EqualTo((byte)'S'));
            var restored = System.Text.Encoding.UTF8.GetString(bytes);
            var rows = ReadRows(restored);
            Assert.That(rows[1][0], Is.EqualTo("café-source"));
            Assert.That(rows[1][3], Is.EqualTo("Région"));
        }

        [TestCase(ImportStatus.Completed)]
        [TestCase(ImportStatus.Failed)]
        [TestCase(ImportStatus.Stopped)]
        [TestCase(ImportStatus.Canceled)]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-038")]
        [Property("Scenario", "FR-038-SC01")]
        public void FR_038_SC01_ExportsExistingDiagnosticsWithoutChangingOutcome(ImportStatus status)
        {
            var row = ImportDiagnostic.ForRecord(1, null, DiagnosticCodes.ValidationFailed);
            ImportResult outcome;
            if (status == ImportStatus.Failed)
                outcome = ImportResult.Failed(1, new[] { row,
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.SourceUnreadable) });
            else if (status == ImportStatus.Stopped)
                outcome = ImportResult.Stopped(1, new[] { row });
            else if (status == ImportStatus.Canceled)
                outcome = ImportResult.Canceled(1, new[] { row });
            else
                outcome = ImportResult.Completed(1, new[] { row });
            var result = new TypedImportResult<string>("result-source", outcome,
                new string[0], "do not print caller status");
            var rows = ReadRows(new CsvImportResultSerializerAdapter().Serialize(result));
            Assert.That(rows.Count, Is.EqualTo(status == ImportStatus.Failed ? 3 : 2));
            Assert.That(rows[1][2], Is.EqualTo("1"));
            Assert.That(rows[1][3], Is.Empty);
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
        }

        [TestCase(";")]
        [TestCase("|")]
        [TestCase("\t")]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-038")]
        [Property("Scenario", "FR-038-SC01")]
        public void FR_038_SC01_CustomDelimiterRoundTripsTextAndSourceMetadata(string delimiter)
        {
            var diagnostic = ImportDiagnostic.ForRecord(1, "Column" + delimiter + "B",
                DiagnosticCodes.RequiredFieldMissing);
            var result = new TypedImportResult<string>("safe" + delimiter + "batch",
                ImportResult.Completed(1, new[] { diagnostic }),
                new string[0], "private-status");
            var rows = ReadRows(new CsvImportResultSerializerAdapter(delimiter[0])
                .Serialize(result), delimiter);
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[1][0], Is.EqualTo("safe" + delimiter + "batch"));
            Assert.That(rows[1][3], Is.EqualTo("Column" + delimiter + "B"));
            Assert.That(rows[1][5], Is.EqualTo(diagnostic.Message));
        }

        [TestCase('"')]
        [TestCase('\n')]
        [TestCase('\r')]
        [TestCase('\0')]
        [Property("Requirement", "FR-038")]
        public void InvalidCsvDelimiterIsRejected(char delimiter)
        {
            Assert.Throws<ArgumentException>(() => new CsvImportResultSerializerAdapter(delimiter));
        }

        [TestCase("=1+2")]
        [TestCase("+1+2")]
        [TestCase("-1+2")]
        [TestCase("@SUM(1,2)")]
        [TestCase("\t=1+2")]
        [TestCase("\r\n=1+2")]
        [TestCase("=1+2,second-field")]
        [TestCase("\"=1+2,\"")]
        [Property("Requirement", "FR-038")]
        public void FormulaLikeMetadataIsLosslesslyPreservedForMachineCsvConsumers(string metadata)
        {
            // Interchange CSV preserves caller-controlled metadata as data; it does
            // not promise to neutralize formulas in spreadsheet applications.
            // A real CSV parser validates field boundaries, not spreadsheet behavior.
            var diagnostic = ImportDiagnostic.ForRecord(1, metadata,
                DiagnosticCodes.ValidationFailed, metadata, 4, null);
            var result = new TypedImportResult<string>(metadata,
                ImportResult.Completed(1, new[] { diagnostic }),
                new string[0], "do not export");

            var csv = new CsvImportResultSerializerAdapter().Serialize(result);
            var rows = ReadRows(csv);

            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[1].Length, Is.EqualTo(9));
            Assert.That(rows[1][0], Is.EqualTo(metadata));
            Assert.That(rows[1][3], Is.EqualTo(metadata));
            Assert.That(rows[1][6], Is.EqualTo(metadata));
            Assert.That(rows[1][8], Is.Empty);
            Assert.That(csv, Does.Not.Contain("do not export"));
        }

        [Test]
        [Property("Requirement", "FR-038")]
        public void NullResultIsRejectedAndAbsoluteSourcePathIsRedacted()
        {
            var serializer = new CsvImportResultSerializerAdapter();
            Assert.Throws<ArgumentNullException>(() => serializer.Serialize<string>(null));
            var result = new TypedImportResult<string>(
                "C:\\private-directory\\sensitive-file.csv",
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.SourceUnreadable)
                }), new string[0], "private-message");
            var rows = ReadRows(serializer.Serialize(result));
            Assert.That(rows[1][0], Is.EqualTo("source-redacted"));
            Assert.That(rows[1][5], Is.EqualTo(result.Diagnostics[0].Message));
        }

        [Test]
        [Property("Requirement", "FR-038")]
        public void CsvIsDeterministicAcrossAmbientCultures()
        {
            var diagnostic = ImportDiagnostic.ForRecord(1, "Amount",
                DiagnosticCodes.ConversionFailed, null, null, 2002);
            var result = new TypedImportResult<string>("culture",
                ImportResult.Completed(1, new[] { diagnostic }),
                new string[0], "ignored");
            var serializer = new CsvImportResultSerializerAdapter();
            var oldCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var baseline = serializer.Serialize(result);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(serializer.Serialize(result), Is.EqualTo(baseline));
                Assert.That(ReadRows(baseline)[1][8], Is.EqualTo("2002"));
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
            }
        }
    }
}
