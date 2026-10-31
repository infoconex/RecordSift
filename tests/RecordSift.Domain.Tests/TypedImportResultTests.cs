using System;
using System.Collections.Generic;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class TypedImportResultTests
    {
        [Test]
        public void TypedResultSnapshotsAcceptedRecordsAndExposesExistingSummary()
        {
            var records = new List<string> { "first", "second" };
            var result = new TypedImportResult<string>("source-01",
                ImportResult.Completed(2, new ImportDiagnostic[0]),
                records, "Completed");
            records.Add("third");

            Assert.That(result.SourceId, Is.EqualTo("source-01"));
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.AcceptedRecords, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(((IList<string>)result.AcceptedRecords).IsReadOnly, Is.True);
        }

        [Test]
        public void AcceptedRecordCountMustMatchExistingResultInvariants()
        {
            Assert.Throws<ArgumentException>(() => new TypedImportResult<string>(
                "source-01", ImportResult.Completed(2, new ImportDiagnostic[0]),
                new[] { "only-one" }, "Completed"));
        }

        [Test]
        public void SourceIdAndUserMessageMustBeNonEmpty()
        {
            var outcome = ImportResult.Completed(0, new ImportDiagnostic[0]);
            Assert.Throws<ArgumentException>(() => new TypedImportResult<string>(
                "", outcome, new string[0], "Completed"));
            Assert.Throws<ArgumentException>(() => new TypedImportResult<string>(
                "source-01", outcome, new string[0], " "));
        }

        [Test]
        public void FailedSourceCannotAppearAsACompletedTypedImport()
        {
            var sourceFailure = ImportResult.Failed(0, new[] {
                ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing)
            });
            var result = new TypedImportResult<string>("source-01", sourceFailure,
                new string[0], "Failed");

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-036")]
        [Property("Scenario", "FR-036-SC01")]
        public void FR_036_SC01_TypedAcceptedRecordsAndStructuredDiagnosticsAreAccessibleWithoutJson()
        {
            var diagnostics = new List<ImportDiagnostic> {
                ImportDiagnostic.ForRecord(2, "Name",
                    DiagnosticCodes.ValidationFailed, null, null, 4)
            };
            var accepted = new List<string> { "Alice", "Carol" };
            var result = new TypedImportResult<string>("source-036",
                ImportResult.Completed(3, diagnostics), accepted,
                "Completed with rejected records");
            accepted.Add("not part of snapshot");
            diagnostics.Add(ImportDiagnostic.ForRecord(2, "Id", DiagnosticCodes.ValidationFailed));

            Assert.That(result.SourceId, Is.EqualTo("source-036"));
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.AcceptedRecords, Is.EqualTo(new[] { "Alice", "Carol" }));
            Assert.That(((IList<string>)result.AcceptedRecords).IsReadOnly, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(4));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-036")]
        [Property("Scenario", "FR-036-SC02")]
        public void FR_036_SC02_MultipleDiagnosticsCountOneRejectedRowAndSourceFailureRemainsDistinct()
        {
            var result = new TypedImportResult<string>("source-036",
                ImportResult.Completed(3, new[] {
                    ImportDiagnostic.ForRecord(2, "Id", DiagnosticCodes.ValidationFailed),
                    ImportDiagnostic.ForRecord(2, "Name", DiagnosticCodes.RequiredFieldMissing)
                }), new[] { "Alice", "Carol" }, "Completed with errors");

            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Name"));
            Assert.That(typeof(ImportSummary).GetProperty("ErrorCount"), Is.Null);
            Assert.That(typeof(ImportSummary).GetProperty("WarningCount"), Is.Null);
            Assert.That(typeof(ImportSummary).GetProperty("WarningRowCount"), Is.Null);

            var failed = new TypedImportResult<string>("empty-source",
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing)
                }), new string[0], "Required source header missing");
            Assert.That(failed.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(failed.Summary.ProcessedCount, Is.Zero);
            Assert.That(failed.Summary.RejectedCount, Is.Zero);
            Assert.That(failed.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(failed.Diagnostics[0].Row, Is.Null);
            Assert.That(failed.AcceptedRecords, Is.Empty);
        }
    }
}
