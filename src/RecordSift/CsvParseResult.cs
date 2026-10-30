using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift
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

        public bool IsSuccessful { get; private set; }
        public IReadOnlyList<CsvRecord> Records { get; private set; }
        public IReadOnlyList<ImportDiagnostic> Diagnostics { get; private set; }

        public static CsvParseResult Success(IEnumerable<CsvRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            return new CsvParseResult(new ReadOnlyCollection<CsvRecord>(
                new List<CsvRecord>(records)),
                new ReadOnlyCollection<ImportDiagnostic>(new List<ImportDiagnostic>()));
        }

        public static CsvParseResult Failure(ImportDiagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));
            if (diagnostic.Scope != DiagnosticScope.Source)
                throw new ArgumentException("CSV parsing errors must be source-scoped.", nameof(diagnostic));
            return new CsvParseResult(
                new ReadOnlyCollection<CsvRecord>(new List<CsvRecord>()),
                new ReadOnlyCollection<ImportDiagnostic>(new List<ImportDiagnostic> { diagnostic }));
        }
    }
}
