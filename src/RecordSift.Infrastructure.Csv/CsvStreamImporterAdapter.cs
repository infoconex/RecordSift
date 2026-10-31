using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Messages;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Infrastructure.Csv
{
    /// <summary>
    /// Decodes caller-owned CSV byte streams before invoking the format-neutral
    /// TextReader importer. No encoding or decoder implementation enters Domain.
    /// </summary>
    public sealed class CsvStreamImporterAdapter<T>
    {
        private readonly CsvImporter<T> _importer;

        public CsvStreamImporterAdapter(CsvImporter<T> importer)
        {
            if (importer == null) throw new ArgumentNullException(nameof(importer));
            _importer = importer;
        }

        /// <summary>
        /// Uses an explicit encoding with deterministic strict decoding by default.
        /// The source stream remains owned by the caller. BOMs never override the
        /// configured encoding.
        /// </summary>
        public TypedImportResult<T> Import(Stream source, int maxCharacters,
            Encoding encoding, ImportSchema schema, CsvHeaderPolicy policy,
            string sourceId, bool strictDecoding = true,
            IEnumerable<SourceColumnPosition> positions = null, CsvReadOptions options = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            // Clone before setting the decoder fallback; never mutate a caller's Encoding.
            var selectedEncoding = (Encoding)encoding.Clone();
            selectedEncoding.DecoderFallback = strictDecoding
                ? DecoderFallback.ExceptionFallback
                : DecoderFallback.ReplacementFallback;

            try
            {
                using (var reader = new StreamReader(source, selectedEncoding, false, 1024, true))
                    return _importer.Import(reader, maxCharacters, schema,
                        policy, sourceId, positions, options);
            }
            catch (DecoderFallbackException)
            {
                return FailedSource(sourceId, DiagnosticCodes.SourceDecodingFailed);
            }
            catch (IOException)
            {
                return FailedSource(sourceId, DiagnosticCodes.SourceUnreadable);
            }
        }

        private static TypedImportResult<T> FailedSource(string sourceId, string diagnosticCode)
        {
            return new TypedImportResult<T>(sourceId,
                ImportResult.Failed(0, new[] {
                    ImportDiagnostic.ForSource(null, diagnosticCode)
                }), new T[0], CsvMessageResources.ImportFailed);
        }
    }
}
