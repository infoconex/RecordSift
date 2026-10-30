using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Results
{
    /// <summary>
    /// Immutable import outcome and ordered accepted CLR records. The accepted
    /// collection is snapshotted; the caller still owns each record instance.
    /// </summary>
    public sealed class TypedImportResult<T>
    {
        public TypedImportResult(string sourceId, ImportResult outcome,
            IEnumerable<T> acceptedRecords, string message)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException(DomainMessageResources.ImportSourceIdBlank, nameof(sourceId));
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            if (acceptedRecords == null) throw new ArgumentNullException(nameof(acceptedRecords));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException(DomainMessageResources.ImportMessageBlank, nameof(message));

            var snapshot = new List<T>(acceptedRecords);
            if (snapshot.Count != outcome.Summary.AcceptedCount)
                throw new ArgumentException(DomainMessageResources.AcceptedRecordCountMismatch,
                    nameof(acceptedRecords));

            SourceId = sourceId;
            Outcome = outcome;
            AcceptedRecords = new ReadOnlyCollection<T>(snapshot);
            Message = message;
        }

        /// <summary>Caller-controlled opaque identifier; never an implicit file path.</summary>
        public string SourceId { get; private set; }
        public ImportResult Outcome { get; private set; }
        public ImportStatus Status => Outcome.Status;
        public ImportSummary Summary => Outcome.Summary;
        public IReadOnlyList<ImportDiagnostic> Diagnostics => Outcome.Diagnostics;
        public IReadOnlyList<T> AcceptedRecords { get; private set; }
        public string Message { get; private set; }
    }
}
