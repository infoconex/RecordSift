using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    public abstract class CsvRecordReaderContractTests
    {
        protected abstract ICsvRecordReader CreateReader();

        private CsvParseResult Parse(string text, int maxCharacters = 4096)
        {
            using (var reader = new StringReader(text))
                return CreateReader().Read(reader, maxCharacters);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC03")]
        public void FR_001_SC03_UnclosedQuotedFieldIsSourceFailureWithPhysicalLocation()
        {
            var result = Parse("Id,Value\n1,\"unfinished\nvalue");
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-001")]
        [Property("Scenario", "FR-001-SC04")]
        public void FR_001_SC04_QuotedCommaNewlineAndEscapedQuoteStayInOneField()
        {
            var result = Parse("Id,Description\r\n1,\"hello,\r\n\"\"world\"\"\"\r\n2,done");
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(3));
            Assert.That(result.Records[1].Fields[0], Is.EqualTo("1"));
            Assert.That(result.Records[1].Fields[1], Is.EqualTo("hello,\r\n\"world\""));
            Assert.That(result.Records[1].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Records[2].PhysicalLine, Is.EqualTo(4));
            Assert.That(result.Records[2].SourceRecordNumber, Is.EqualTo(3));
        }

        [Test]
        public void ParserDoesNotInferHeaderOrDiscardFirstSourceRecord()
        {
            var result = Parse("Id,Name\n1,A\n2,B\n3,C");
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(4));
            Assert.That(result.Records[0].Fields, Is.EqualTo(new[] { "Id", "Name" }));
            Assert.That(result.Records[1].Fields, Is.EqualTo(new[] { "1", "A" }));
            Assert.That(result.Records[3].Fields, Is.EqualTo(new[] { "3", "C" }));
            Assert.That(result.Records[0].SourceRecordNumber, Is.EqualTo(1));
            Assert.That(result.Records[3].SourceRecordNumber, Is.EqualTo(4));
        }

        [Test]
        public void ZeroByteTextSourceHasNoRawRecordsOrErrors()
        {
            var result = Parse("");
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void TrailingEmptyFieldsAreNotDropped()
        {
            var result = Parse("first,,third,");
            Assert.That(result.Records[0].Fields, Is.EqualTo(new[] { "first", "", "third", "" }));
        }

        [Test]
        public void QuotedEmptyAndEscapedQuotesAreParsed()
        {
            var result = Parse("\"\",\"\"\"\",end");
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records[0].Fields, Is.EqualTo(new[] { "", "\"", "end" }));
        }

        [Test]
        public void LineNumbersCountCrLfAsOnePhysicalLine()
        {
            var result = Parse("a\r\nb\nc\rd");
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(4));
            Assert.That(result.Records[0].PhysicalLine, Is.EqualTo(1));
            Assert.That(result.Records[1].PhysicalLine, Is.EqualTo(2));
            Assert.That(result.Records[2].PhysicalLine, Is.EqualTo(3));
            Assert.That(result.Records[3].PhysicalLine, Is.EqualTo(4));
        }

        [Test]
        public void FinalLineTerminatorDoesNotAddPhantomRecord()
        {
            var result = Parse("A,B\r\nC,D\r\n");
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(2));
        }

        [Test]
        public void UnexpectedQuoteInUnquotedFieldFailsWithoutReturningPartialRows()
        {
            var result = Parse("good,row\nbad,un\"quoted");
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
        }

        [Test]
        public void CharactersAfterClosingQuoteAreRejected()
        {
            var result = Parse("\"value\"unexpected,other");
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
        }

        [Test]
        public void MalformedDiagnosticNeverEchoesRawSourceText()
        {
            const string Secret = "api-key=secret123";
            var result = Parse("\"" + Secret);
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain(Secret));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("secret123"));
        }

        [Test]
        public void SourceReadBudgetIsEnforcedAsClassifiedFailure()
        {
            var result = Parse("123456", 5);
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Records, Is.Empty);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceLimitExceeded));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
        }

        [Test]
        public void ExactCharacterBudgetCanSucceed()
        {
            var result = Parse("A,B", 3);
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Records[0].Fields, Is.EqualTo(new[] { "A", "B" }));
        }

        private sealed class ThrowingReader : TextReader
        {
            public override int Read() { throw new IOException("Secret: /private/customer.csv"); }
        }

        [Test]
        public void SourceIoExceptionIsClassifiedWithoutExposingPaths()
        {
            using (var source = new ThrowingReader())
            {
                var result = CreateReader().Read(source, 20);
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.SourceUnreadable));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("/private/customer.csv"));
            }
        }

        [Test]
        public void NullSourceAndInvalidBudgetAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => CreateReader().Read(null, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => Parse("data", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Parse("data", -1));
        }

        [Test]
        public void UnclosedQuoteAfterEarlierMultilineFieldUsesCurrentPhysicalLine()
        {
            var result = Parse("\"first\nfield\",\"second\nunfinished");
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.MalformedSource));
            Assert.That(result.Diagnostics[0].PhysicalLine, Is.EqualTo(2));
        }

        [Test]
        public void ReaderDoesNotDisposeCallerOwnedTextReader()
        {
            using (var text = new StringReader("a,b"))
            {
                CreateReader().Read(text, 10);
                Assert.DoesNotThrow(() => text.Peek());
            }
        }

        [Test]
        public void RecordFieldCollectionsAndResultCollectionsAreImmutable()
        {
            var result = Parse("1,2");
            Assert.That(((IList<CsvRecord>)result.Records).IsReadOnly, Is.True);
            Assert.That(((IList<string>)result.Records[0].Fields).IsReadOnly, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        }
    }
}
