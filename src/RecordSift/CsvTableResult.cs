using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift
{
    /// <summary>
    /// Raw data rows after CSV header policy and schema binding. This is not a
    /// typed/validated ImportResult, so accepted/rejected counters are not inferred.
    /// </summary>
    public sealed class CsvTableResult
    {
        private CsvTableResult(ImportStatus status, IEnumerable<CsvDataRow> dataRows,
            IEnumerable<SourceColumnBinding> bindings, IEnumerable<ImportDiagnostic> diagnostics)
        {
            if (dataRows == null) throw new ArgumentNullException(nameof(dataRows));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            Status = status;
            DataRows = new ReadOnlyCollection<CsvDataRow>(new List<CsvDataRow>(dataRows));
            Bindings = new ReadOnlyCollection<SourceColumnBinding>(new List<SourceColumnBinding>(bindings));
            Diagnostics = new ReadOnlyCollection<ImportDiagnostic>(new List<ImportDiagnostic>(diagnostics));
        }

        public ImportStatus Status { get; private set; }
        public IReadOnlyList<CsvDataRow> DataRows { get; private set; }
        public IReadOnlyList<SourceColumnBinding> Bindings { get; private set; }
        public IReadOnlyList<ImportDiagnostic> Diagnostics { get; private set; }

        internal static CsvTableResult Completed(IEnumerable<CsvDataRow> dataRows,
            IEnumerable<SourceColumnBinding> bindings)
        {
            return new CsvTableResult(ImportStatus.Completed, dataRows, bindings,
                new ImportDiagnostic[0]);
        }

        internal static CsvTableResult Failed(IEnumerable<ImportDiagnostic> diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            var snapshot = new List<ImportDiagnostic>(diagnostics);
            if (snapshot.Count == 0)
                throw new ArgumentException("Failed CSV table must identify a source error.", nameof(diagnostics));
            foreach (var error in snapshot)
            {
                if (error == null || error.Scope != DiagnosticScope.Source)
                    throw new ArgumentException("Failed CSV table requires source errors.", nameof(diagnostics));
            }

            return new CsvTableResult(ImportStatus.Failed,
                new CsvDataRow[0], new SourceColumnBinding[0], snapshot);
        }
    }
}
