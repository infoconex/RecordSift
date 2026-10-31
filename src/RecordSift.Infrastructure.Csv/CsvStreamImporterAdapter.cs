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
    /// <typeparam name="T">Accepted CLR record type.</typeparam>
    public sealed class CsvStreamImporterAdapter<T>
    {
        // StreamReader's leave-open constructor requires an explicit buffer size.
        private const int StreamReaderBufferSize = 1024;

        private readonly CsvImporter<T> _importer;

        /// <summary>Wraps a CSV importer with deterministic byte decoding; the supplied importer and stream remain caller-owned.</summary>
        /// <param name="importer">Previously configured application CSV importer; cannot be null.</param>
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
            return Import(source, maxCharacters, encoding, schema, policy, sourceId,
                strictDecoding, positions, options, false);
        }

        /// <summary>Imports explicitly decoded CSV with opt-in fail-fast validation, retaining the caller's source stream.</summary>
        /// <param name="source">Caller-owned binary stream.</param>
        /// <param name="maxCharacters">Positive decoded-character input budget.</param>
        /// <param name="encoding">Caller-selected text encoding.</param>
        /// <param name="schema">Declared source schema.</param>
        /// <param name="policy">Source header policy.</param>
        /// <param name="sourceId">Opaque source identifier.</param>
        /// <param name="strictDecoding">Whether invalid bytes reject the input.</param>
        /// <param name="positions">Optional positional source bindings.</param>
        /// <param name="options">Optional CSV parser options.</param>
        /// <param name="failFast">Stop row validation after its first rejected record.</param>
        public TypedImportResult<T> Import(Stream source, int maxCharacters,
            Encoding encoding, ImportSchema schema, CsvHeaderPolicy policy,
            string sourceId, bool strictDecoding,
            IEnumerable<SourceColumnPosition> positions, CsvReadOptions options,
            bool failFast)
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
                using (var reader = new StreamReader(source, selectedEncoding, false,
                    StreamReaderBufferSize, true))
                    return _importer.Import(reader, maxCharacters, schema,
                        policy, sourceId, positions, options, failFast);
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
