using System;
using System.IO;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Infrastructure.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvAdversarialSecurityTests
    {
        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-007")]
        public void FixedSeedMalformedCorpusHasOnlyAtomicOrCompleteOutcomes()
        {
            // Tiny reproducible fuzz corpus. This exercises error handling,
            // not a throughput or process-memory guarantee.
            var random = new Random(20261031);
            var alphabet = new[] { 'a', 'b', ',', '"', '\r', '\n', '\0',
                '\u00e9', '\u4e2d', ' ' };
            var parser = new CsvRecordReaderAdapter();
            for (var caseIndex = 0; caseIndex < 96; caseIndex++)
            {
                var length = 1 + random.Next(192);
                var characters = new char[length];
                for (var index = 0; index < length; index++)
                    characters[index] = alphabet[random.Next(alphabet.Length)];

                var csv = new string(characters);
                if (caseIndex % 8 == 0)
                    csv += "SYNTHETIC_TEST_SECRET";

                using (var source = new StringReader(csv))
                {
                    var result = parser.Read(source, 512);
                    Assert.DoesNotThrow(() => source.Peek(),
                        "Source remains caller-owned for case " + caseIndex);
                    if (result.IsSuccessful)
                    {
                        Assert.That(result.Diagnostics, Is.Empty,
                            "Complete result for case " + caseIndex);
                    }
                    else
                    {
                        Assert.That(result.Records, Is.Empty,
                            "Atomic rejection for case " + caseIndex);
                        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                        Assert.That(result.Diagnostics[0].Scope,
                            Is.EqualTo(DiagnosticScope.Source));
                        Assert.That(result.Diagnostics[0].Message,
                            Does.Not.Contain("SYNTHETIC_TEST_SECRET"));
                    }
                }
            }
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-007")]
        public void SingleCharacterPartialReadsPreserveRecordValuesAndLocations()
        {
            const string Csv = "Id,Name\r\n1,\"Ada, Jr.\"\r\n2,\"Multi\nLine\"\r\n";
            var parser = new CsvRecordReaderAdapter();
            using (var ordinary = new StringReader(Csv))
            using (var partial = new SingleCharacterReadReader(Csv))
            {
                var baseline = parser.Read(ordinary, Csv.Length);
                var tested = parser.Read(partial, Csv.Length);
                Assert.That(baseline.IsSuccessful, Is.True);
                Assert.That(tested.IsSuccessful, Is.True);
                Assert.That(tested.Records.Count, Is.EqualTo(baseline.Records.Count));
                for (var index = 0; index < tested.Records.Count; index++)
                {
                    Assert.That(tested.Records[index].Fields,
                        Is.EqualTo(baseline.Records[index].Fields));
                    Assert.That(tested.Records[index].PhysicalLine,
                        Is.EqualTo(baseline.Records[index].PhysicalLine));
                }
                Assert.DoesNotThrow(() => partial.Peek());
            }
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-006")]
        public void PartialReadThenIoExceptionIsSanitizedAndAtomic()
        {
            using (var source = new ThrowingReadReader(
                "Id,Name\r\n1,SYNTHETIC_TEST_SECRET\r\n2,Bob"))
            {
                var result = new CsvRecordReaderAdapter().Read(source, 256);
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.Records, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.SourceUnreadable));
                Assert.That(result.Diagnostics[0].Message,
                    Does.Not.Contain("SYNTHETIC_TEST_SECRET"));
                Assert.DoesNotThrow(() => source.Peek());
            }
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-007")]
        public void UnicodeSurrogatePairConsumesTwoDecodedCharacterBudgetUnits()
        {
            const string Csv = "Name,\ud83d\ude00";
            using (var exact = new StringReader(Csv))
            using (var shortBudget = new StringReader(Csv))
            {
                var parser = new CsvRecordReaderAdapter();
                var complete = parser.Read(exact, Csv.Length);
                var rejected = parser.Read(shortBudget, Csv.Length - 1);
                Assert.That(complete.IsSuccessful, Is.True);
                Assert.That(complete.Records, Has.Count.EqualTo(1));
                Assert.That(complete.Records[0].Fields[1],
                    Is.EqualTo("\ud83d\ude00"));
                Assert.That(rejected.IsSuccessful, Is.False);
                Assert.That(rejected.Records, Is.Empty);
                Assert.That(rejected.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.SourceLimitExceeded));
                Assert.DoesNotThrow(() => exact.Peek());
                Assert.DoesNotThrow(() => shortBudget.Peek());
            }
        }

        private sealed class SingleCharacterReadReader : StringReader
        {
            internal SingleCharacterReadReader(string value) : base(value) { }

            public override int Read(char[] buffer, int index, int count)
            {
                return base.Read(buffer, index, Math.Min(count, 1));
            }
        }

        private sealed class ThrowingReadReader : StringReader
        {
            private int _readCount;

            internal ThrowingReadReader(string value) : base(value) { }

            public override int Read(char[] buffer, int index, int count)
            {
                if (++_readCount > 1)
                    throw new IOException("SYNTHETIC_TEST_SECRET");
                return base.Read(buffer, index, Math.Min(count, 3));
            }
        }
    }
}
