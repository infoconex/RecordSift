using System;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class CsvReaderPortTests
    {
        private sealed class StubReader : ICsvRecordReader
        {
            public int CallCount { get; private set; }

            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                CallCount++;
                return CsvParseResult.Success(new[] {
                    new CsvRecord(1, 1, new[] { "Id", "Name" }),
                    new CsvRecord(2, 2, new[] { "1", "Alice" })
                });
            }
        }

        private sealed class FailingReader : ICsvRecordReader
        {
            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(
                    null, DiagnosticCodes.MalformedSource));
            }
        }

        [Test]
        public void ReaderPortCanBeFakedWithoutAnyThirdPartyParserOrReadingTheStream()
        {
            var fake = new StubReader();
            using (var source = new StringReader("unused"))
            {
                var schema = new ImportSchema(new[] {
                    new ExpectedColumn("Id", true),
                    new ExpectedColumn("Name", true)
                });
                var result = CsvTableReader.Read(fake, source, 128,
                    schema, CsvHeaderPolicy.WithHeader);
                Assert.That(fake.CallCount, Is.EqualTo(1));
                Assert.That(source.Peek(), Is.EqualTo('u'));
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.DataRows[0].Fields, Is.EqualTo(new[] { "1", "Alice" }));
            }
        }

        [Test]
        public void ReaderPortFailureIsTranslatedWithoutReturningPartialRows()
        {
            using (var source = new StringReader("unused"))
            {
                var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
                var result = CsvTableReader.Read(new FailingReader(), source, 128,
                    schema, CsvHeaderPolicy.WithHeader);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.DataRows, Is.Empty);
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.MalformedSource));
            }
        }

        [Test]
        public void ReaderPortCannotBeNull()
        {
            using (var source = new StringReader("unused"))
            {
                var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
                Assert.Throws<ArgumentNullException>(() =>
                    CsvTableReader.Read(null, source, 128, schema, CsvHeaderPolicy.WithHeader));
            }
        }
    }
}
