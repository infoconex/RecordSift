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
    }
}
