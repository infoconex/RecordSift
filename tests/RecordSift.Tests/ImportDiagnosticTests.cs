using System;
using NUnit.Framework;

namespace RecordSift.Tests
{
    [TestFixture]
    public sealed class ImportDiagnosticTests
    {
        // FR-028 unit-contract tests: integration with actual validation and CSV readers is pending.
        [Test]
        public void FR_028_SC01_RecordDiagnosticIdentifiesLogicalRowFieldAndLocation()
        {
            var error = ImportDiagnostic.ForRecord(2, "CustomerId",
                DiagnosticCodes.ValidationFailed, "Orders", 7, null);
            var result = ImportResult.Completed(2, new[] { error });

            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(error.Row, Is.EqualTo(2));
            Assert.That(error.Field, Is.EqualTo("CustomerId"));
            Assert.That(error.Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(error.Worksheet, Is.EqualTo("Orders"));
            Assert.That(error.PhysicalRow, Is.EqualTo(7));
            Assert.That(error.PhysicalLine, Is.Null);
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
        }

        [Test]
        public void FR_028_SC02_IndependentErrorsRemainSeparateWithDistinctRowAccounting()
        {
            var a = ImportDiagnostic.ForRecord(1, "Amount", DiagnosticCodes.ConversionFailed);
            var b = ImportDiagnostic.ForRecord(1, "Id", DiagnosticCodes.RequiredFieldMissing);
            var c = ImportDiagnostic.ForRecord(3, "Name", DiagnosticCodes.ValidationFailed);
            var result = ImportResult.Completed(4, new[] { a, b, c });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.Diagnostics, Is.EqualTo(new[] { a, b, c }));
            Assert.That(result.Summary.ProcessedCount, Is.EqualTo(4));
            Assert.That(result.Summary.RejectedCount, Is.EqualTo(2));
            Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
        }

        [Test]
        public void FR_028_SC03_SourceErrorDoesNotRejectDataRecords()
        {
            var error = ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing);
            var result = ImportResult.Failed(0, new[] { error });

            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(error.Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(error.Row, Is.Null);
            Assert.That(result.Summary.ProcessedCount, Is.Zero);
            Assert.That(result.Summary.RejectedCount, Is.Zero);
        }

        [Test]
        public void SafeDefaultMessageUsesOnlyTrustedRegistryTemplate()
        {
            var one = ImportDiagnostic.ForRecord(1, "CustomerId", DiagnosticCodes.ValidationFailed);
            var two = ImportDiagnostic.ForRecord(2, "OrderId", DiagnosticCodes.ValidationFailed);
            Assert.That(one.Message, Is.EqualTo("A field failed validation."));
            Assert.That(two.Message, Is.EqualTo(one.Message));
        }

        [Test]
        public void SourceDiagnosticCanCarryKnownColumnAndPhysicalLine()
        {
            var error = ImportDiagnostic.ForSource("CustomerId",
                DiagnosticCodes.RequiredHeaderMissing, null, null, 1);
            Assert.That(error.Field, Is.EqualTo("CustomerId"));
            Assert.That(error.Worksheet, Is.Null);
            Assert.That(error.PhysicalLine, Is.EqualTo(1));
        }

        [Test]
        public void RecordErrorMayOmitUnavailableFieldAndPhysicalCoordinates()
        {
            var error = ImportDiagnostic.ForRecord(1, null, DiagnosticCodes.ValidationFailed);
            Assert.That(error.Field, Is.Null);
            Assert.That(error.Worksheet, Is.Null);
            Assert.That(error.PhysicalRow, Is.Null);
            Assert.That(error.PhysicalLine, Is.Null);
        }

        [TestCase("INVALID_FIELD")]
        [TestCase("NOT_A_REGISTERED_CODE")]
        [TestCase("validation_failed")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownOrInvalidCodesAreRejected(string code)
        {
            Assert.Throws<ArgumentException>(() => ImportDiagnostic.ForRecord(1, "Id", code));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void LogicalRowMustBeOneBased(int row)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ImportDiagnostic.ForRecord(row, "Id", DiagnosticCodes.ValidationFailed));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void PhysicalCoordinatesMustBePositiveWhenPresent(int coordinate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ImportDiagnostic.ForRecord(1, "Id", DiagnosticCodes.ValidationFailed,
                    null, coordinate, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ImportDiagnostic.ForRecord(1, "Id", DiagnosticCodes.ValidationFailed,
                    null, null, coordinate));
        }

        [Test]
        public void InvalidWorksheetAndFieldMetadataAreRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                ImportDiagnostic.ForRecord(1, " ", DiagnosticCodes.ValidationFailed));
            Assert.Throws<ArgumentException>(() =>
                ImportDiagnostic.ForRecord(1, null, DiagnosticCodes.ValidationFailed, " ", null, null));
        }

        [Test]
        public void RegistryDistinguishesSourceAndRecordCodes()
        {
            Assert.Throws<ArgumentException>(() =>
                ImportDiagnostic.ForRecord(1, "Id", DiagnosticCodes.RequiredHeaderMissing));
            Assert.Throws<ArgumentException>(() =>
                ImportDiagnostic.ForSource(null, DiagnosticCodes.ConversionFailed));
        }

        [Test]
        public void DiagnosticPropertiesAreNotPubliclySettable()
        {
            foreach (var property in typeof(ImportDiagnostic).GetProperties())
                Assert.That(property.GetSetMethod(), Is.Null, property.Name);
        }

        [Test]
        public void ResultOrderAndSnapshotRemainStable()
        {
            var first = ImportDiagnostic.ForRecord(2, "B", DiagnosticCodes.ValidationFailed);
            var second = ImportDiagnostic.ForRecord(1, "A", DiagnosticCodes.ValidationFailed);
            var input = new[] { first, second };
            var result = ImportResult.Completed(2, input);
            input[0] = second;
            Assert.That(result.Diagnostics[0], Is.SameAs(first));
            Assert.That(result.Diagnostics[1], Is.SameAs(second));
        }
    }
}
