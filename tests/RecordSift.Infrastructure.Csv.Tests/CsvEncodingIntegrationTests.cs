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
    }
}
