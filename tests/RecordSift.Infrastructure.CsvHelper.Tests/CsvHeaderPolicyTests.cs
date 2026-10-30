using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RecordSift.Infrastructure.CsvHelper;
using RecordSift;

namespace RecordSift.Infrastructure.CsvHelper.Tests
{
    [TestFixture]
    public sealed class CsvHeaderPolicyTests
    {
        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Id", true), new ExpectedColumn("Name", true)
            });
        }

        private static CsvTableResult Read(string csv, CsvHeaderPolicy policy,
            IEnumerable<SourceColumnPosition> positions = null)
        {
            using (var text = new StringReader(csv))
                return CsvTableReader.Read(new CsvHelperRecordReader(), text, 4096, Schema(), policy, positions);
        }

        [Test]
        public void FR_004_SC01_HeadedAndHeaderlessCsvRetainIdenticalDataRows()
        {
            var headed = Read("Name,Id\nAlice,1\nBob,2", CsvHeaderPolicy.WithHeader);
            var headerless = Read("Alice,1\nBob,2", CsvHeaderPolicy.WithoutHeader,
                new[] { new SourceColumnPosition("Name", 0), new SourceColumnPosition("Id", 1) });

            Assert.That(headed.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headerless.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(headed.DataRows, Has.Count.EqualTo(2));
            Assert.That(headerless.DataRows, Has.Count.EqualTo(2));
            Assert.That(headed.DataRows[0].Fields, Is.EqualTo(headerless.DataRows[0].Fields));
            Assert.That(headed.DataRows[1].Fields, Is.EqualTo(headerless.DataRows[1].Fields));
            Assert.That(headed.DataRows[0].Row, Is.EqualTo(1));
            Assert.That(headerless.DataRows[0].Row, Is.EqualTo(1));
            Assert.That(headed.DataRows[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(headerless.DataRows[0].PhysicalLine, Is.EqualTo(1));
            Assert.That(headed.Bindings[0].SourceIndex, Is.EqualTo(1));
            Assert.That(headerless.Bindings[0].SourceIndex, Is.EqualTo(1));
        }

        [Test]
        public void FR_004_SC02_MissingRequiredCsvHeaderFailsWithoutBindingAnyData()
        {
            var result = Read("Id\n1\n2", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.DataRows, Is.Empty);
            Assert.That(result.Bindings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredColumnMissing));
        }

        [Test]
        public void EmptyHeadedSourceFailsWithRequiredHeaderDiagnostic()
        {
            var result = Read("", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.DataRows, Is.Empty);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredHeaderMissing));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
        }

        [Test]
        public void ZeroByteHeaderlessSourceSucceedsWithNoData()
        {
            var result = Read("", CsvHeaderPolicy.WithoutHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.DataRows, Is.Empty);
            Assert.That(result.Bindings, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void HeaderOnlyCsvIsSuccessfulWithZeroDataRows()
        {
            var result = Read("Name,Id\r\n", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.DataRows, Is.Empty);
            Assert.That(result.Bindings, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void HeaderlessFirstRowIsNeverSilentlyDiscarded()
        {
            var result = Read("A,1\nB,2", CsvHeaderPolicy.WithoutHeader, new[] {
                new SourceColumnPosition("Name", 0), new SourceColumnPosition("Id", 1)
            });
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.DataRows[0].Fields[0], Is.EqualTo("A"));
            Assert.That(result.DataRows[0].Row, Is.EqualTo(1));
            Assert.That(result.DataRows[0].SourceRecordNumber, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateRequiredHeaderIsAmbiguousAndCannotShiftValues()
        {
            var result = Read("Id,Name,Id\n1,A,2", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.DataRows, Is.Empty);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.AmbiguousColumn));
        }

        [Test]
        public void QuotedNewlineRetainsPhysicalLineAndLogicalDataNumber()
        {
            var result = Read("Id,Name\n1,\"A\nB\"\n2,C", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
            Assert.That(result.DataRows, Has.Count.EqualTo(2));
            Assert.That(result.DataRows[0].Row, Is.EqualTo(1));
            Assert.That(result.DataRows[0].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.DataRows[1].Row, Is.EqualTo(2));
            Assert.That(result.DataRows[1].PhysicalLine, Is.EqualTo(4));
        }

        [Test]
        public void TruncatedDataRowFailsBeforeMisreadingPosition()
        {
            var result = Read("Id,Name\n1,A\n2", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.DataRows, Is.Empty);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.InvalidColumnPosition));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
        }

        [Test]
        public void InvalidHeaderlessSourcePositionFailsAtSourceScope()
        {
            var result = Read("1,A", CsvHeaderPolicy.WithoutHeader,
                new[] { new SourceColumnPosition("Id", 0), new SourceColumnPosition("Name", 2) });
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.InvalidColumnPosition));
        }

        [Test]
        public void MalformedSourceParsingIsReportedWithoutPartialRows()
        {
            var result = Read("Id,Name\n1,\"unfinished", CsvHeaderPolicy.WithHeader);
            Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.DataRows, Is.Empty);
        }

        [Test]
        public void HeaderlessDataRequiresExplicitPositionMapping()
        {
            Assert.Throws<ArgumentException>(() => Read("1,A", CsvHeaderPolicy.WithoutHeader));
        }

        [Test]
        public void HeadedModeRejectsConflictingPositionalConfiguration()
        {
            Assert.Throws<ArgumentException>(() => Read("Id,Name\n1,A", CsvHeaderPolicy.WithHeader,
                new[] { new SourceColumnPosition("Id", 0) }));
        }

        [Test]
        public void InvalidHeaderPolicyAndNullSchemaAreRejected()
        {
            using (var text = new StringReader("Id,Name\n1,A"))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    CsvTableReader.Read(new CsvHelperRecordReader(), text, 100, Schema(), (CsvHeaderPolicy)0));
                Assert.Throws<ArgumentNullException>(() =>
                    CsvTableReader.Read(new CsvHelperRecordReader(), text, 100, null, CsvHeaderPolicy.WithHeader));
            }
        }

        [Test]
        public void TableResultDataAndBindingCollectionsAreReadOnly()
        {
            var result = Read("Id,Name\n1,A", CsvHeaderPolicy.WithHeader);
            Assert.That(((IList<CsvDataRow>)result.DataRows).IsReadOnly, Is.True);
            Assert.That(((IList<SourceColumnBinding>)result.Bindings).IsReadOnly, Is.True);
            Assert.That(result.DataRows[0].Fields, Is.EqualTo(new[] { "1", "A" }));
        }
    }
}
