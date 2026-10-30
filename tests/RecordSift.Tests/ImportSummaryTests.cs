using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RecordSift.Tests
{
    [TestFixture]
    public sealed class ImportSummaryTests
    {
        private static ImportDiagnostic RecordError(int row, string field = "CustomerId")
        {
            return ImportDiagnostic.ForRecord(row, field, "INVALID_FIELD", "Field validation failed.");
        }

        private static ImportDiagnostic SourceError()
        {
            return ImportDiagnostic.ForSource(null, "MISSING_HEADER", "Required header row is missing.");
        }

        [Test]
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
    }
}
