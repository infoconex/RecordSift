using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Application.Messages;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;

namespace RecordSift.Application.Ports.Csv
{
    /// <summary>
    /// Result of raw CSV tokenization only. This is not a typed ImportResult and
    /// does not provide accepted/rejected data-row counters.
    /// </summary>
    public sealed class CsvParseResult
    {
        private CsvParseResult(IReadOnlyList<CsvRecord> records,
            IReadOnlyList<ImportDiagnostic> diagnostics)
        {
            Records = records;
            Diagnostics = diagnostics;
            IsSuccessful = diagnostics.Count == 0;
        }

        /// <summary>Gets whether parsing finished without a source-level error.</summary>
        public bool IsSuccessful { get; private set; }
        /// <summary>Gets a read-only snapshot of raw CSV records, including a possible header; empty on failure.</summary>
        public IReadOnlyList<CsvRecord> Records { get; private set; }
        /// <summary>Gets source-scope parser errors as a read-only snapshot.</summary>
        public IReadOnlyList<ImportDiagnostic> Diagnostics { get; private set; }

        /// <summary>Creates a successful parse result by snapshotting the ordered raw records.</summary>
        /// <param name="records">Ordered raw source records, snapshotted by the result.</param>
        public static CsvParseResult Success(IEnumerable<CsvRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            return new CsvParseResult(new ReadOnlyCollection<CsvRecord>(
                new List<CsvRecord>(records)),
                new ReadOnlyCollection<ImportDiagnostic>(new List<ImportDiagnostic>()));
        }

        /// <summary>Creates an atomic parse failure containing exactly one source-scoped diagnostic and no records.</summary>
        /// <param name="diagnostic">One registered source-scope error.</param>
        public static CsvParseResult Failure(ImportDiagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));
            if (diagnostic.Scope != DiagnosticScope.Source)
                throw new ArgumentException(CsvMessageResources.ParserErrorScope, nameof(diagnostic));
            return new CsvParseResult(
                new ReadOnlyCollection<CsvRecord>(new List<CsvRecord>()),
                new ReadOnlyCollection<ImportDiagnostic>(new List<ImportDiagnostic> { diagnostic }));
        }
    }
}
