using System;
using System.Collections.Generic;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class ImportSummaryTests
    {
        private static ImportDiagnostic RecordError(int row, string field = "CustomerId")
        {
            return ImportDiagnostic.ForRecord(row, field, DiagnosticCodes.ValidationFailed);
        }

        private static ImportDiagnostic SourceError()
        {
            return ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-030")]
        [Property("Scenario", "FR-030-SC01")]
        public void FR_030_SC01_CompletedSummaryCountsDistinctRejectedRows()
        {
            var result = ImportResult.Completed(4, new[]
            {
                RecordError(2), RecordError(2, "Name"), RecordError(4)
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(4));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.Summary.ProcessedCount,
                Is.EqualTo(result.Summary.AcceptedCount + result.Summary.RejectedCount));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-030")]
        [Property("Scenario", "FR-030-SC02")]
        public void FR_030_SC02_OptionalBlankFieldWithoutErrorsCountsAsAccepted()
        {
            // FR-012 owns optional-field validation; a validated row with no errors
            // contributes to accepted, never to warnings or rejected counts.
            var result = ImportResult.Completed(1, new ImportDiagnostic[0]);
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.Zero);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-030")]
        [Property("Scenario", "FR-030-SC03")]
        public void FR_030_SC03_CompletedEmptyAndMissingHeaderFailureDifferByStatus()
        {
            var empty = ImportResult.Completed(0, new ImportDiagnostic[0]);
            var noHeader = ImportResult.Failed(0, new[] { SourceError() });

            Assert.That(empty.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(noHeader.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(empty.Summary.ProcessedCount, Is.Zero);
            Assert.That(empty.Summary.AcceptedCount, Is.Zero);
            Assert.That(empty.Summary.RejectedCount, Is.Zero);
            Assert.That(noHeader.Summary.ProcessedCount, Is.Zero);
            Assert.That(noHeader.Summary.AcceptedCount, Is.Zero);
            Assert.That(noHeader.Summary.RejectedCount, Is.Zero);
            Assert.That(empty.Diagnostics, Is.Empty);
            Assert.That(noHeader.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(noHeader.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(noHeader.Diagnostics[0].Row, Is.Null);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-030")]
        [Property("Scenario", "FR-030-SC04")]
        public void FR_030_SC04_MultipleErrorsPerRowDoNotInflateRejectedCount()
        {
            var result = ImportResult.Completed(5, new[]
            {
                RecordError(2), RecordError(2, "Name"), RecordError(2, "Age"),
                RecordError(4)
            });

            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(5));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(4));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[2].Field, Is.EqualTo("Age"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-030")]
        [Property("Scenario", "FR-030-SC05")]
        public void FR_030_SC05_RejectedRowsStillCompleteImportProcessing()
        {
            var result = ImportResult.Completed(5, new[]
            {
                RecordError(2), RecordError(5)
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(5));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(3));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
        }

        [Test]
        public void NegativeProcessedCountIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ImportResult.Completed(-1, new ImportDiagnostic[0]));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void RecordErrorsMustReferToProcessedLogicalRows(int row)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ImportResult.Completed(2, new[] { RecordError(row) }));
        }

        [Test]
        public void CompletedImportRejectsFatalSourceDiagnostics()
        {
            Assert.Throws<ArgumentException>(() =>
                ImportResult.Completed(0, new[] { SourceError() }));
        }

        [Test]
        public void FailedImportRequiresSourceDiagnostic()
        {
            Assert.Throws<ArgumentException>(() =>
                ImportResult.Failed(2, new[] { RecordError(1) }));
        }

        [Test]
        public void FailedImportDoesNotCountSourceErrorsAsRejectedRows()
        {
            var result = ImportResult.Failed(3, new[]
            {
                RecordError(2), SourceError()
            });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
        }

        [Test]
        public void ImportResultSnapshotsDiagnosticSequence()
        {
            var errors = new List<ImportDiagnostic> { RecordError(1) };
            var result = ImportResult.Completed(1, errors);
            errors.Add(RecordError(1, "Name"));

            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
        }

        [Test]
        public void NullDiagnosticsAreRejectedInsteadOfDisappearing()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ImportResult.Completed(1, null));
            Assert.Throws<ArgumentException>(() =>
                ImportResult.Completed(1, new ImportDiagnostic[] { null }));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-031")]
        [Property("Scenario", "FR-031-SC01")]
        public void FR_031_SC01_StoppedOutcomePreservesPartialCountsAndFirstRecordFailure()
        {
            var error = ImportDiagnostic.ForRecord(2, "Name", DiagnosticCodes.ValidationFailed);
            var result = ImportResult.Stopped(2, new[] { error });
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Stopped));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Row, Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() =>
                ImportResult.Stopped(2, new ImportDiagnostic[0]));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_CanceledOutcomeRetainsProcessedPrefixWithoutInventingError()
        {
            var canceled = ImportResult.Canceled(2, new[] {
                ImportDiagnostic.ForRecord(2, "Name", DiagnosticCodes.ValidationFailed)
            });
            Assert.That(canceled.Status, Is.EqualTo(ImportStatus.Canceled));
            Assert.That(canceled.Summary.ProcessedCount, Is.EqualTo(2));
            Assert.That(canceled.Summary.AcceptedCount, Is.EqualTo(1));
            Assert.That(canceled.Summary.RejectedCount, Is.EqualTo(1));
            var beforeFirst = ImportResult.Canceled(0, new ImportDiagnostic[0]);
            Assert.That(beforeFirst.Status, Is.EqualTo(ImportStatus.Canceled));
            Assert.That(beforeFirst.Diagnostics, Is.Empty);
            Assert.That(beforeFirst.Summary.ProcessedCount, Is.Zero);
            Assert.Throws<ArgumentException>(() => ImportResult.Canceled(0,
                new[] { ImportDiagnostic.ForSource(null, DiagnosticCodes.SourceUnreadable) }));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_CanceledOutcomeRetainsActualPartialCountsWithoutSourceFailure()
        {
            var empty = ImportResult.Canceled(0, new ImportDiagnostic[0]);
            Assert.That(empty.Status, Is.EqualTo(ImportStatus.Canceled));
            Assert.That(empty.Summary.ProcessedCount, Is.Zero);
            Assert.That(empty.Diagnostics, Is.Empty);

            var partial = ImportResult.Canceled(3, new[] {
                ImportDiagnostic.ForRecord(2, "Name", DiagnosticCodes.ValidationFailed)
            });
            Assert.That(partial.Status, Is.EqualTo(ImportStatus.Canceled));
            Assert.That(partial.Summary.ProcessedCount, Is.EqualTo(3));
            Assert.That(partial.Summary.AcceptedCount, Is.EqualTo(2));
            Assert.That(partial.Summary.RejectedCount, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => ImportResult.Canceled(0,
                new[] { ImportDiagnostic.ForSource(null, DiagnosticCodes.SourceUnreadable) }));
        }
    }
}
