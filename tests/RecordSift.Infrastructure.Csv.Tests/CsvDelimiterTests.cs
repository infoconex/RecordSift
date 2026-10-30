using System;
using System.IO;
using NUnit.Framework;
using RecordSift;
using RecordSift.Infrastructure.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;
using RecordSift.Domain.Results;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Application.Importing.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    public sealed class CsvDelimiterTests
    {
        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Description", true)
            });
        }

        [Test]
        public void FR_003_SC01_SemicolonDelimiterPreservesQuotedSemicolonInRealAdapter()
        {
            using (var source = new StringReader("Id;Description\r\n1;\"hello;world\"\r\n2;done"))
            {
                var result = CsvTableReader.Read(new CsvRecordReaderAdapter(), source, 200,
                    Schema(), CsvHeaderPolicy.WithHeader, options: new CsvReadOptions(";"));
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.DataRows, Has.Count.EqualTo(2));
                Assert.That(result.DataRows[0].Fields, Is.EqualTo(new[] { "1", "hello;world" }));
                Assert.That(result.DataRows[1].Fields, Is.EqualTo(new[] { "2", "done" }));
                Assert.That(result.DataRows[0].PhysicalLine, Is.EqualTo(2));
            }
        }

        [TestCase("\t")]
        [TestCase("|")]
        [TestCase("^")]
        public void AlternateSingleCharacterDelimitersAreSupported(string delimiter)
        {
            using (var source = new StringReader("a" + delimiter + "\"b" + delimiter + "c\""))
            {
                var result = new CsvRecordReaderAdapter().Read(
                    source, 100, new CsvReadOptions(delimiter));
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(result.Records[0].Fields, Is.EqualTo(new[] { "a", "b" + delimiter + "c" }));
            }
        }

        [Test]
        public void DefaultConfigurationIsCommaAndDoesNotInferSemicolon()
        {
            using (var source = new StringReader("a,b"))
            {
                var result = new CsvRecordReaderAdapter().Read(source, 100);
                Assert.That(result.Records[0].Fields, Is.EqualTo(new[] { "a", "b" }));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("||")]
        [TestCase("\"")]
        [TestCase("\r")]
        [TestCase("\n")]
        [TestCase("\0")]
        public void FR_003_SC02_InvalidDelimiterReturnsSafeSourceDiagnosticBeforeReading(string delimiter)
        {
            using (var source = new StringReader("never read"))
            {
                var result = new CsvRecordReaderAdapter().Read(source, 100,
                    new CsvReadOptions(delimiter));
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.Records, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.InvalidCsvDelimiter));
                Assert.That(result.Diagnostics[0].Message,
                    Is.EqualTo("The configured CSV delimiter is invalid."));
                Assert.That(source.Peek(), Is.EqualTo('n'));
            }
        }

        [Test]
        public void InvalidDelimiterFailsInCoreBeforeCallingParserPort()
        {
            var reader = new TrackingReader();
            using (var source = new StringReader("Id,Description\n1,valid"))
            {
                var result = CsvTableReader.Read(reader, source, 100,
                    Schema(), CsvHeaderPolicy.WithHeader,
                    options: new CsvReadOptions(";;"));
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.InvalidCsvDelimiter));
                Assert.That(reader.ReadCalled, Is.False);
                Assert.That(source.Peek(), Is.EqualTo('I'));
            }
        }

        [Test]
        public void ExplicitDelimiterDoesNotChangeInvariantSourceRecordLocations()
        {
            using (var source = new StringReader("A;B\r\n1;\"hello\r\nworld\"\r\n2;C"))
            {
                var result = new CsvRecordReaderAdapter().Read(
                    source, 300, new CsvReadOptions(";"));
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(result.Records, Has.Count.EqualTo(3));
                Assert.That(result.Records[1].PhysicalLine, Is.EqualTo(2));
                Assert.That(result.Records[2].PhysicalLine, Is.EqualTo(4));
            }
        }

        private sealed class TrackingReader : ICsvRecordReader
        {
            public bool ReadCalled { get; private set; }

            public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
            {
                ReadCalled = true;
                throw new InvalidOperationException("Invalid configuration must not reach this reader.");
            }
        }
    }
}
