using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Infrastructure.Reporting;

namespace RecordSift.Infrastructure.Reporting.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class JsonReportAcceptanceTests
    {
        private static IDictionary<string, object> Parse(string json)
        {
            return (IDictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json);
        }

        private static IDictionary<string, object> Fields(object value)
        {
            return (IDictionary<string, object>)value;
        }

        private static object[] Items(object value)
        {
            return (object[])value;
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-035")]
        [Property("Scenario", "FR-035-SC01")]
        public void FR_035_SC01_JsonContainsVersionedResultSchemaAndOrderedDiagnostics()
        {
            var result = new TypedImportResult<string>("source-035",
                ImportResult.Completed(3, new[] {
                    ImportDiagnostic.ForRecord(2, "Name",
                        DiagnosticCodes.ValidationFailed, null, null, 4),
                    ImportDiagnostic.ForRecord(2, "Id",
                        DiagnosticCodes.RequiredFieldMissing)
                }), new[] { "Alice", "Carol" }, "Import completed with errors.");
            var json = new JsonImportResultSerializerAdapter().Serialize(result);
            var report = Parse(json);
            Assert.That(report.Keys, Is.EquivalentTo(new[] {
                "schemaVersion", "sourceId", "status", "message", "summary", "diagnostics"
            }));
            Assert.That(report["schemaVersion"], Is.EqualTo("1.0"));
            Assert.That(report["sourceId"], Is.EqualTo("source-035"));
            Assert.That(report["status"], Is.EqualTo("Completed"));
            Assert.That(report["message"], Is.EqualTo("Import completed with rejected records."));
            var summary = Fields(report["summary"]);
            Assert.That(summary.Keys, Is.EquivalentTo(new[] {
                "processedCount", "acceptedCount", "rejectedCount"
            }));
            Assert.That(summary["processedCount"], Is.EqualTo(3));
            Assert.That(summary["acceptedCount"], Is.EqualTo(2));
            Assert.That(summary["rejectedCount"], Is.EqualTo(1));
            var errors = Items(report["diagnostics"]);
            Assert.That(errors, Has.Length.EqualTo(2));
            Assert.That(Fields(errors[0])["scope"], Is.EqualTo("Record"));
            Assert.That(Fields(errors[0])["row"], Is.EqualTo(2));
            Assert.That(Fields(errors[0])["field"], Is.EqualTo("Name"));
            Assert.That(Fields(errors[0])["code"], Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(Fields(errors[0])["physicalLine"], Is.EqualTo(4));
            Assert.That(Fields(errors[1])["field"], Is.EqualTo("Id"));
            Assert.That(Fields(errors[1]).ContainsKey("physicalLine"), Is.False);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-035")]
        [Property("Scenario", "FR-035-SC02")]
        public void FR_035_SC02_JsonUsesInMemoryResultWithoutOriginalSource()
        {
            var result = new TypedImportResult<int>("missing-original-source",
                ImportResult.Completed(1, new ImportDiagnostic[0]),
                new[] { 42 }, "Import completed.");
            var serializer = new JsonImportResultSerializerAdapter();
            var json = serializer.Serialize(result);
            Assert.That(serializer.Serialize(result), Is.EqualTo(json));
            var report = Parse(json);
            Assert.That(Fields(report["summary"])["acceptedCount"], Is.EqualTo(1));
            Assert.That(Items(report["diagnostics"]), Is.Empty);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-035")]
        [Property("Scenario", "FR-035-SC03")]
        public void FR_035_SC03_JsonExcludesRawRecordsAndEscapesOpaqueIdentifier()
        {
            const string PrivateValue = "person-secret-123";
            const string SafeSourceId = "batch-\"quoted\"\\two";
            var result = new TypedImportResult<string>(SafeSourceId,
                ImportResult.Completed(2, new[] {
                    ImportDiagnostic.ForRecord(1, "Name", DiagnosticCodes.ValidationFailed)
                }), new[] { PrivateValue }, "Completed with errors.");
            var json = new JsonImportResultSerializerAdapter().Serialize(result);
            var report = Parse(json);
            Assert.That(json, Does.Not.Contain(PrivateValue));
            Assert.That(json, Does.Not.Contain("acceptedRecords"));
            Assert.That(json, Does.Not.Contain("C:\\Users\\"));
            Assert.That(report["sourceId"], Is.EqualTo(SafeSourceId));
            Assert.That(Fields(Items(report["diagnostics"])[0])["message"],
                Is.EqualTo("A field failed validation."));

            var unsafeResult = new TypedImportResult<string>(
                @"C:\Users\private\source.csv",
                ImportResult.Completed(2, new[] {
                    ImportDiagnostic.ForRecord(1, "Name", DiagnosticCodes.ValidationFailed)
                }), new[] { PrivateValue }, "Report contains " + PrivateValue);
            var unsafeJson = new JsonImportResultSerializerAdapter().Serialize(unsafeResult);
            var safeReport = Parse(unsafeJson);
            Assert.That(unsafeJson, Does.Not.Contain(PrivateValue));
            Assert.That(unsafeJson, Does.Not.Contain(@"C:\Users\"));
            Assert.That(safeReport["sourceId"], Is.EqualTo("source-redacted"));
            Assert.That(safeReport["message"], Is.EqualTo("Import completed with rejected records."));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-035")]
        [Property("Scenario", "FR-035-SC04")]
        public void FR_035_SC04_JsonCountsRejectedRowsOnceWithoutWarningFields()
        {
            var result = new TypedImportResult<int>("report-035",
                ImportResult.Completed(4, new[] {
                    ImportDiagnostic.ForRecord(1, "Id", DiagnosticCodes.ValidationFailed),
                    ImportDiagnostic.ForRecord(1, "Name", DiagnosticCodes.RequiredFieldMissing),
                    ImportDiagnostic.ForRecord(3, "Age", DiagnosticCodes.FieldRangeOutOfRange)
                }), new[] { 7, 8 }, "Completed with errors.");
            var report = Parse(new JsonImportResultSerializerAdapter().Serialize(result));
            var summary = Fields(report["summary"]);
            Assert.That(summary["processedCount"], Is.EqualTo(4));
            Assert.That(summary["acceptedCount"], Is.EqualTo(2));
            Assert.That(summary["rejectedCount"], Is.EqualTo(2));
            Assert.That(summary.ContainsKey("errorCount"), Is.False);
            Assert.That(summary.ContainsKey("warningRowCount"), Is.False);
            Assert.That(Items(report["diagnostics"]), Has.Length.EqualTo(3));
            foreach (var diagnostic in Items(report["diagnostics"]))
                Assert.That(Fields(diagnostic).ContainsKey("severity"), Is.False);
        }

        [Test]
        public void FatalSourceDiagnosticSerializesNullRecordAndOptionalMetadataAbsence()
        {
            var result = new TypedImportResult<int>("missing-headers",
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing)
                }), new int[0], "Required header missing.");
            var report = Parse(new JsonImportResultSerializerAdapter().Serialize(result));
            Assert.That(report["status"], Is.EqualTo("Failed"));
            var error = Fields(Items(report["diagnostics"])[0]);
            Assert.That(error["scope"], Is.EqualTo("Source"));
            Assert.That(error["row"], Is.Null);
            Assert.That(error["field"], Is.Null);
            Assert.That(error.ContainsKey("physicalLine"), Is.False);
        }

        [Test]
        [Property("Requirement", "FR-031")]
        public void StoppedOutcomeUsesVersionedJsonStatusAndSanitizedMessage()
        {
            var stopped = new TypedImportResult<string>("fr031-report",
                ImportResult.Stopped(2, new[] {
                    ImportDiagnostic.ForRecord(2, "Name",
                        DiagnosticCodes.ValidationFailed, null, null, 3)
                }), new[] { "private-accepted" }, "private-status");
            var json = new JsonImportResultSerializerAdapter().Serialize(stopped);
            var report = Parse(json);
            Assert.That(report["schemaVersion"], Is.EqualTo("1.1"));
            Assert.That(report["status"], Is.EqualTo("Stopped"));
            Assert.That(Fields(report["summary"])["processedCount"], Is.EqualTo(2));
            Assert.That(json, Does.Not.Contain("private-accepted"));
            Assert.That(json, Does.Not.Contain("private-status"));
            Assert.That((string)report["message"], Does.Contain("stopped"));
        }
    }
}
