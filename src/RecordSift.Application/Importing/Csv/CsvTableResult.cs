using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Application.Messages;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Importing.Csv
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

        /// <summary>Gets whether source binding succeeded or failed.</summary>
        public ImportStatus Status { get; private set; }
        /// <summary>Gets a read-only snapshot of logical data rows after header processing; empty on failure.</summary>
        public IReadOnlyList<CsvDataRow> DataRows { get; private set; }
        /// <summary>Gets read-only ordered source column bindings; empty on failure.</summary>
        public IReadOnlyList<SourceColumnBinding> Bindings { get; private set; }
        /// <summary>Gets read-only source-scope diagnostics when processing failed.</summary>
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
                throw new ArgumentException(CsvMessageResources.FailedTableNoError, nameof(diagnostics));
            foreach (var error in snapshot)
            {
                if (error == null || error.Scope != DiagnosticScope.Source)
                    throw new ArgumentException(CsvMessageResources.FailedTableNonSourceError, nameof(diagnostics));
            }

            return new CsvTableResult(ImportStatus.Failed,
                new CsvDataRow[0], new SourceColumnBinding[0], snapshot);
        }
    }
}
