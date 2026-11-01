using System;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Infrastructure.Reporting;

namespace RecordSift.Infrastructure.Reporting.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class DiagnosticSecrecySecurityTests
    {
        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-006")]
        public void AcceptedValuesAndCallerStatusMessagesAreAbsentFromAllReports()
        {
            const string RecordSecret = "SYNTHETIC_RECORD_SECRET";
            const string StatusSecret = "SYNTHETIC_STATUS_SECRET";
            var result = new TypedImportResult<string>("opaque-input",
                ImportResult.Completed(1, new ImportDiagnostic[0]),
                new[] { RecordSecret }, StatusSecret);

            var reports = new[] {
                new JsonImportResultSerializerAdapter().Serialize(result),
                new CsvImportResultSerializerAdapter().Serialize(result),
                new ReadableImportResultFormatterAdapter().Format(result)
            };
            foreach (var report in reports)
            {
                Assert.That(report, Does.Not.Contain(RecordSecret));
                Assert.That(report, Does.Not.Contain(StatusSecret));
            }
        }

        [TestCase("C:/SYNTHETIC_TEST_SECRET/source.csv")]
        [TestCase("/tmp/SYNTHETIC_TEST_SECRET/source.csv")]
        [TestCase("file:///SYNTHETIC_TEST_SECRET/source.csv")]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-006")]
        public void RootedSourceIdsAreRedactedInMachineReadableReports(
            string untrustedSourceId)
        {
            var result = new TypedImportResult<string>(untrustedSourceId,
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.MalformedSource)
                }), new string[0], "caller status is not exported");
            var json = new JsonImportResultSerializerAdapter().Serialize(result);
            var csv = new CsvImportResultSerializerAdapter().Serialize(result);

            Assert.That(json, Does.Contain("source-redacted"));
            Assert.That(csv, Does.Contain("source-redacted"));
            Assert.That(json, Does.Not.Contain("SYNTHETIC_TEST_SECRET"));
            Assert.That(csv, Does.Not.Contain("SYNTHETIC_TEST_SECRET"));
            Assert.That(json, Does.Not.Contain("source.csv"));
            Assert.That(csv, Does.Not.Contain("source.csv"));
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-006")]
        public void OpaqueMetadataIsPreservedButSafeDiagnosticMessagesRemainSanitized()
        {
            // Source, field and worksheet identifiers are caller-owned metadata.
            // They intentionally are NOT redacted under the documented contract.
            const string Metadata = "SYNTHETIC_METADATA_TOKEN";
            var diagnostic = ImportDiagnostic.ForRecord(1,
                "Field-" + Metadata, DiagnosticCodes.ValidationFailed,
                "Worksheet-" + Metadata, 3, null);
            var result = new TypedImportResult<string>("opaque-" + Metadata,
                ImportResult.Completed(1, new[] { diagnostic }),
                new string[0], "caller status");

            var json = new JsonImportResultSerializerAdapter().Serialize(result);
            var csv = new CsvImportResultSerializerAdapter().Serialize(result);
            var readable = new ReadableImportResultFormatterAdapter().Format(result);
            Assert.That(json, Does.Contain("opaque-" + Metadata));
            Assert.That(csv, Does.Contain("opaque-" + Metadata));
            foreach (var report in new[] { json, csv, readable })
            {
                Assert.That(report, Does.Contain("Field-" + Metadata));
                Assert.That(report, Does.Contain("Worksheet-" + Metadata));
            }
            Assert.That(diagnostic.Message, Does.Not.Contain(Metadata));
        }
    }
}
