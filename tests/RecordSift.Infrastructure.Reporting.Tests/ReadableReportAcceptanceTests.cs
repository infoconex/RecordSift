using System;
using System.Globalization;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Infrastructure.Reporting;

namespace RecordSift.Infrastructure.Reporting.Tests
{
    [TestFixture]
    [Category("Component")]
    public sealed class ReadableReportAcceptanceTests
    {
        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-037")]
        [Property("Scenario", "FR-037-SC01")]
        public void FR_037_SC01_ReadableSummaryExplainsOrderedInvalidRowsWithoutLeakingRecordValues()
        {
            const string PrivateRecordValue = "private-customer-123";
            var result = new TypedImportResult<string>("safe-source",
                ImportResult.Completed(3, new[] {
                    ImportDiagnostic.ForRecord(2, "Name",
                        DiagnosticCodes.ValidationFailed, null, null, 4),
                    ImportDiagnostic.ForRecord(2, "Id", DiagnosticCodes.RequiredFieldMissing)
                }), new[] { "Alice", PrivateRecordValue },
                "Untrusted: " + PrivateRecordValue);
            var text = new ReadableImportResultFormatterAdapter().Format(result);

            Assert.That(text, Does.Contain("Import completed"));
            Assert.That(text, Does.Contain("3 processed"));
            Assert.That(text, Does.Contain("2 accepted"));
            Assert.That(text, Does.Contain("1 rejected"));
            Assert.That(text, Does.Contain("2 errors"));
            Assert.That(text, Does.Contain("Record 2"));
            Assert.That(text, Does.Contain("Name"));
            Assert.That(text, Does.Contain("Id"));
            Assert.That(text, Does.Contain(DiagnosticCodes.ValidationFailed));
            Assert.That(text, Does.Contain(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(text, Does.Contain("physical line 4"));
            Assert.That(text, Does.Not.Contain(PrivateRecordValue));
            Assert.That(text, Does.Not.Contain("Untrusted:"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-037")]
        [Property("Scenario", "FR-037-SC02")]
        public void FR_037_SC02_CleanImportReportsCountsWithoutInventedErrors()
        {
            var result = new TypedImportResult<string>("safe-source",
                ImportResult.Completed(2, new ImportDiagnostic[0]),
                new[] { "secret-first", "secret-second" }, "Untrusted summary.");
            var text = new ReadableImportResultFormatterAdapter().Format(result);
            Assert.That(text, Does.Contain("Import completed"));
            Assert.That(text, Does.Contain("2 processed"));
            Assert.That(text, Does.Contain("2 accepted"));
            Assert.That(text, Does.Contain("0 rejected"));
            Assert.That(text, Does.Contain("0 errors"));
            Assert.That(text, Does.Not.Contain("secret-first"));
            Assert.That(text, Does.Not.Contain("Untrusted summary"));
            Assert.That(text, Does.Not.Contain("Import failed"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-037")]
        [Property("Scenario", "FR-037-SC03")]
        public void FR_037_SC03_EmptySuccessAndRequiredHeaderSourceFailureHaveDistinctReadableOutcomes()
        {
            var empty = new TypedImportResult<string>("source",
                ImportResult.Completed(0, new ImportDiagnostic[0]),
                new string[0], "Caller-supplied secret.");
            var failed = new TypedImportResult<string>("source",
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing)
                }), new string[0], "Caller-supplied secret.");
            var formatter = new ReadableImportResultFormatterAdapter();
            var emptyText = formatter.Format(empty);
            var failedText = formatter.Format(failed);
            Assert.That(empty.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(failed.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(emptyText, Does.Contain("No data records found"));
            Assert.That(emptyText, Does.Not.Contain("Import failed"));
            Assert.That(failedText, Does.Contain("Import failed"));
            Assert.That(failedText, Does.Contain("required source header is missing"));
            Assert.That(failedText, Does.Contain(DiagnosticCodes.RequiredHeaderMissing));
            Assert.That(failedText, Does.Not.Contain("No data records found"));
            Assert.That(failedText, Does.Not.Contain("Caller-supplied secret"));
        }

        [Test]
        public void NullResultIsRejectedAndReadableReportIsInvariantAcrossCultures()
        {
            var formatter = new ReadableImportResultFormatterAdapter();
            Assert.Throws<ArgumentNullException>(() => formatter.Format<string>(null));
            var result = new TypedImportResult<int>("safe-source",
                ImportResult.Completed(2, new ImportDiagnostic[0]),
                new[] { 1, 2 }, "Ignore");
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                var baseline = formatter.Format(result);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(formatter.Format(result), Is.EqualTo(baseline));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }
    }
}
