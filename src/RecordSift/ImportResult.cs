using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift
{
    /// <summary>
    /// In-memory status, row summary and supplied error diagnostics.
    /// This is not yet the FR-036 JSON report or the FR-027 typed-record result.
    /// </summary>
    public sealed class ImportResult
    {
        private ImportResult(ImportStatus status, ImportSummary summary,
            IReadOnlyList<ImportDiagnostic> diagnostics)
        {
            Status = status;
            Summary = summary;
            Diagnostics = diagnostics;
        }

        public ImportStatus Status { get; private set; }
        public ImportSummary Summary { get; private set; }
        public IReadOnlyList<ImportDiagnostic> Diagnostics { get; private set; }

        /// <summary>Processing finished, possibly with rejected data records.</summary>
        public static ImportResult Completed(int processedCount, IEnumerable<ImportDiagnostic> diagnostics)
        {
            return Create(ImportStatus.Completed, processedCount, diagnostics);
        }

        /// <summary>A source/schema failure occurred, with any preceding processed rows retained.</summary>
        public static ImportResult Failed(int processedCount, IEnumerable<ImportDiagnostic> diagnostics)
        {
            return Create(ImportStatus.Failed, processedCount, diagnostics);
        }

        private static ImportResult Create(ImportStatus status, int processedCount,
            IEnumerable<ImportDiagnostic> diagnostics)
        {
            if (processedCount < 0) throw new ArgumentOutOfRangeException(nameof(processedCount));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            var snapshot = new List<ImportDiagnostic>();
            var rejectedRows = new HashSet<int>();
            var hasSourceError = false;

            foreach (var error in diagnostics)
            {
                if (error == null)
                    throw new ArgumentException("Diagnostics cannot contain null.", nameof(diagnostics));

                if (error.Scope == DiagnosticScope.Record)
                {
                    if (!error.Row.HasValue || error.Row.Value > processedCount)
                        throw new ArgumentOutOfRangeException(nameof(diagnostics),
                            "Record diagnostics must identify a processed logical row.");
                    rejectedRows.Add(error.Row.Value);
                }
                else if (error.Scope == DiagnosticScope.Source)
                {
                    hasSourceError = true;
                }
                else
                {
                    throw new ArgumentException("Unrecognized diagnostic scope.", nameof(diagnostics));
                }

                snapshot.Add(error);
            }

            if (status == ImportStatus.Completed && hasSourceError)
                throw new ArgumentException("A completed import cannot contain fatal source errors.", nameof(diagnostics));
            if (status == ImportStatus.Failed && !hasSourceError)
                throw new ArgumentException("A failed import must identify a source-level error.", nameof(diagnostics));

            return new ImportResult(status,
                new ImportSummary(processedCount, rejectedRows.Count),
                new ReadOnlyCollection<ImportDiagnostic>(snapshot));
        }
    }
}
