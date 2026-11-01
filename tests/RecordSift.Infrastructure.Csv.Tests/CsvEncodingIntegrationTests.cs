using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;
using RecordSift.Infrastructure.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    [TestFixture]
    [Category("Integration")]
    public sealed class CsvEncodingIntegrationTests
    {
        private sealed class EncodedCustomer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private static CsvStreamImporterAdapter<EncodedCustomer> CreateImporter(
            out ImportSchema schema)
        {
            schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Name", true)
            });
            var mapping = new PropertyMappingSchema(schema, new[] {
                new SourcePropertyMapping("Id", "Id"),
                new SourcePropertyMapping("Name", "Name")
            });
            var mapper = new PrimitivePropertyMapper<EncodedCustomer>(
                mapping, CultureInfo.InvariantCulture);
            return new CsvStreamImporterAdapter<EncodedCustomer>(
                new CsvImporter<EncodedCustomer>(
                    new CsvRecordReaderAdapter(), mapper.Map));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-006")]
        [Property("Scenario", "FR-006-SC01")]
        public void FR_006_SC01_ExplicitLatin1DecodingPreservesNonAsciiTypedValues()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(
                "Id,Name\r\n1,Café\r\n2,Niño");

            using (var source = new MemoryStream(bytes))
            {
                var result = importer.Import(source, 4096,
                    Encoding.GetEncoding("iso-8859-1"), schema,
                    CsvHeaderPolicy.WithHeader, "latin1-customers");

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.SourceId, Is.EqualTo("latin1-customers"));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(result.AcceptedRecords[0].Id, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Café"));
                Assert.That(result.AcceptedRecords[1].Id, Is.EqualTo(2));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("Niño"));
                Assert.That(source.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-006")]
        [Property("Scenario", "FR-006-SC02")]
        public void FR_006_SC02_StrictUtf8DecoderReportsSanitizedSourceFailure()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            var prefix = Encoding.ASCII.GetBytes("Id,Name\r\n1,private-value\r\n2,");
            var bytes = new byte[prefix.Length + 2];
            Array.Copy(prefix, bytes, prefix.Length);
            bytes[prefix.Length] = 0xC3;
            bytes[prefix.Length + 1] = 0x28;

            using (var source = new MemoryStream(bytes))
            {
                var result = importer.Import(source, 4096, Encoding.UTF8,
                    schema, CsvHeaderPolicy.WithHeader, "invalid-utf8",
                    strictDecoding: true);

                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Summary.AcceptedCount, Is.Zero);
                Assert.That(result.Summary.RejectedCount, Is.Zero);
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.SourceDecodingFailed));
                Assert.That(result.Diagnostics[0].Row, Is.Null);
                Assert.That(result.Diagnostics[0].Field, Is.Null);
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("private-value"));
                Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("0xC3"));
                Assert.That(source.CanRead, Is.True);
            }
        }

        [Test]
        public void ExplicitReplacementDecodingDoesNotReportAConversionError()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            var bytes = new byte[] {
                0x49, 0x64, 0x2c, 0x4e, 0x61, 0x6d, 0x65, 0x0a,
                0x31, 0x2c, 0xc3, 0x28
            };
            using (var source = new MemoryStream(bytes))
            {
                var result = importer.Import(source, 4096, Encoding.UTF8,
                    schema, CsvHeaderPolicy.WithHeader, "replacement",
                    strictDecoding: false);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(1));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics, Is.Empty);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-031")]
        [Property("Scenario", "FR-031-SC01")]
        public void FR_031_SC01_EncodedCsvStreamStopsAtFirstInvalidRowWithoutDisposingCallerStream()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            using (var source = new MemoryStream(Encoding.UTF8.GetBytes(
                "Id,Name\r\n1,Alice\r\ninvalid,private\r\n3,Bob")))
            {
                var result = importer.Import(source, 4096, Encoding.UTF8, schema,
                    CsvHeaderPolicy.WithHeader, "fr031-stream",
                    true, null, null, true);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Stopped));
                Assert.That(result.Summary.ProcessedCount, Is.EqualTo(2));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(1));
                Assert.That(result.Summary.RejectedCount, Is.EqualTo(1));
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Alice"));
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.ConversionFailed));
                Assert.That(source.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-032")]
        [Property("Scenario", "FR-032-SC01")]
        public void FR_032_SC01_PreCanceledEncodedStreamPreservesCallerOwnedBytes()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            using (var cancellation = new System.Threading.CancellationTokenSource())
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(
                "Id,Name\r\n1,Alice\r\n2,Bob")))
            {
                cancellation.Cancel();
                var result = importer.Import(stream, 4096, Encoding.UTF8,
                    schema, CsvHeaderPolicy.WithHeader, "fr032-stream",
                    true, null, null, false, cancellation.Token);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Canceled));
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(stream.Position, Is.Zero);
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-007")]
        public void SingleByteStreamReadsPreserveSplitUtf8Characters()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            var bytes = Encoding.UTF8.GetBytes("Id,Name\\r\\n1,Café\\r\\n2,漢字");
            using (var stream = new SingleByteReadStream(bytes))
            {
                var result = importer.Import(stream, 256, Encoding.UTF8,
                    schema, CsvHeaderPolicy.WithHeader, "partial-utf8");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Completed));
                Assert.That(result.Summary.AcceptedCount, Is.EqualTo(2));
                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(result.AcceptedRecords[0].Name, Is.EqualTo("Café"));
                Assert.That(result.AcceptedRecords[1].Name, Is.EqualTo("漢字"));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-006")]
        public void ByteStreamIoExceptionAfterPartialReadFailsAtomicallyWithoutSecret()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            using (var stream = new FailingByteReadStream(
                Encoding.UTF8.GetBytes("Id,Name\\r\\n1,SYNTHETIC_TEST_SECRET")))
            {
                var result = importer.Import(stream, 256, Encoding.UTF8,
                    schema, CsvHeaderPolicy.WithHeader, "io-failure");
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Summary.ProcessedCount, Is.Zero);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.SourceUnreadable));
                Assert.That(result.Diagnostics[0].Message,
                    Does.Not.Contain("SYNTHETIC_TEST_SECRET"));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        [Property("Verification", "Regression")]
        [Property("Requirement", "NFR-006")]
        public void StrictDecoderFailureDoesNotMutateCallerEncodingFallback()
        {
            ImportSchema schema;
            var importer = CreateImporter(out schema);
            var encoding = (Encoding)Encoding.UTF8.Clone();
            encoding.DecoderFallback = DecoderFallback.ReplacementFallback;
            var originalFallback = encoding.DecoderFallback;
            using (var stream = new MemoryStream(new byte[] {
                0x49, 0x64, 0x2c, 0x4e, 0x61, 0x6d, 0x65, 0x0a,
                0x31, 0x2c, 0xc3, 0x28
            }))
            {
                var result = importer.Import(stream, 256, encoding,
                    schema, CsvHeaderPolicy.WithHeader, "encoding-ownership",
                    strictDecoding: true);
                Assert.That(result.Status, Is.EqualTo(ImportStatus.Failed));
                Assert.That(result.AcceptedRecords, Is.Empty);
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.SourceDecodingFailed));
                Assert.That(stream.CanRead, Is.True);
            }
            Assert.That(encoding.DecoderFallback, Is.SameAs(originalFallback));
        }

        private sealed class SingleByteReadStream : MemoryStream
        {
            internal SingleByteReadStream(byte[] bytes) : base(bytes) { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, 1));
            }
        }

        private sealed class FailingByteReadStream : MemoryStream
        {
            private int _reads;

            internal FailingByteReadStream(byte[] bytes) : base(bytes) { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (++_reads > 1)
                    throw new IOException("SYNTHETIC_TEST_SECRET");
                return base.Read(buffer, offset, Math.Min(count, 3));
            }
        }

    }
}
