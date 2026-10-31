using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;

namespace RecordSift.Infrastructure.Reporting.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class JsonCompatibilityGoldenTests
    {
        private static readonly JsonImportResultSerializerAdapter Serializer =
            new JsonImportResultSerializerAdapter();

        private static void AssertGolden<T>(string fixture, TypedImportResult<T> result)
        {
            string json = Serializer.Serialize(result);
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "Fixtures", "Compatibility", fixture);
            string expectedJson = File.ReadAllText(path);
            var parser = new JavaScriptSerializer();
            AssertSameJson(parser.DeserializeObject(expectedJson),
                parser.DeserializeObject(json), "$");
            Assert.That(Serializer.Serialize(result), Is.EqualTo(json),
                "Repeated serialization must be deterministic.");
            Assert.That(json, Does.Not.Contain("\"severity\""));
            Assert.That(json, Does.Not.Contain("\"warningCount\""));
            Assert.That(json, Does.Not.Contain("\"warningRowCount\""));
        }

        private static void AssertSameJson(object expected, object actual, string location)
        {
            var expectedMap = expected as IDictionary<string, object>;
            if (expectedMap != null)
            {
                var actualMap = actual as IDictionary<string, object>;
                Assert.That(actualMap, Is.Not.Null, location);
                CollectionAssert.AreEquivalent(expectedMap.Keys, actualMap.Keys, location);
                foreach (string key in expectedMap.Keys)
                    AssertSameJson(expectedMap[key], actualMap[key], location + "." + key);
                return;
            }
            var expectedList = expected as object[];
            if (expectedList != null)
            {
                var actualList = actual as object[];
                Assert.That(actualList, Is.Not.Null, location);
                Assert.That(actualList.Length, Is.EqualTo(expectedList.Length), location);
                for (int index = 0; index < expectedList.Length; index++)
                    AssertSameJson(expectedList[index], actualList[index],
                        location + "[" + index + "]");
                return;
            }
            Assert.That(actual, Is.EqualTo(expected), location);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_SuccessfulImportPreservesVersionAndSummary()
        {
            var result = new TypedImportResult<string>("csv-success",
                ImportResult.Completed(2, new ImportDiagnostic[0]),
                new[] { "one", "two" }, "Ignored caller text");
            AssertGolden("successful-import.json", result);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_RejectedRecordsPreserveAllOrderedDiagnostics()
        {
            var diagnostics = new[] {
                ImportDiagnostic.ForRecord(1, "Id", DiagnosticCodes.RequiredFieldMissing),
                ImportDiagnostic.ForRecord(1, "Name", DiagnosticCodes.ValidationFailed),
                ImportDiagnostic.ForRecord(2, "Age", DiagnosticCodes.ConversionFailed)
            };
            var result = new TypedImportResult<string>("csv-rejected",
                ImportResult.Completed(2, diagnostics), new string[0], "Ignored");
            AssertGolden("rejected-records.json", result);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_SourceFailurePreservesExplicitNullRowAndField()
        {
            var result = new TypedImportResult<string>("csv-failed",
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing)
                }), new string[0], "Ignored");
            AssertGolden("source-failure.json", result);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_EmptyCompletedImportPreservesZeroCounters()
        {
            var result = new TypedImportResult<string>("csv-empty",
                ImportResult.Completed(0, new ImportDiagnostic[0]),
                new string[0], "Ignored");
            AssertGolden("empty-import.json", result);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_CsvPhysicalLineRemainsOptionalProvenance()
        {
            var result = new TypedImportResult<string>("csv-provenance",
                ImportResult.Completed(1, new[] {
                    ImportDiagnostic.ForRecord(1, "Name", DiagnosticCodes.ValidationFailed,
                        null, null, 3)
                }), new string[0], "Ignored");
            AssertGolden("csv-provenance.json", result);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_XlsxWorksheetAndPhysicalRowRemainStable()
        {
            var result = new TypedImportResult<string>("xlsx-provenance",
                ImportResult.Completed(1, new[] {
                    ImportDiagnostic.ForRecord(1, "Name", DiagnosticCodes.ValidationFailed,
                        "Data", 5, null)
                }), new string[0], "Ignored");
            AssertGolden("xlsx-provenance.json", result);
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-010")]
        [Property("Scenario", "NFR-010-SC02")]
        public void NFR_010_SC02_XlsxSourceFailureRetainsWorksheetWithoutPhysicalRow()
        {
            var result = new TypedImportResult<string>("xlsx-missing",
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.WorksheetNotFound,
                        "Missing", null, null)
                }), new string[0], "Ignored");
            AssertGolden("xlsx-source-failure.json", result);
        }
    }
}
