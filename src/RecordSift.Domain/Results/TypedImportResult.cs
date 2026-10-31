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
    /// <typeparam name="T">Type of the accepted CLR record instances.</typeparam>
    public sealed class TypedImportResult<T>
    {
        /// <summary>Creates a typed result from a status/outcome, ordered accepted records and caller-provided metadata; accepted record count must match the summary.</summary>
        /// <param name="sourceId">Nonblank caller-owned opaque source identifier.</param>
        /// <param name="outcome">Shared immutable import outcome.</param>
        /// <param name="acceptedRecords">Accepted record instances, snapshotted without cloning elements.</param>
        /// <param name="message">Nonblank caller-provided status text.</param>
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
        /// <summary>Gets the shared immutable import outcome.</summary>
        public ImportResult Outcome { get; private set; }
        /// <summary>Gets the shared completion or source-failure status.</summary>
        public ImportStatus Status => Outcome.Status;
        /// <summary>Gets processed, accepted and rejected logical record counts.</summary>
        public ImportSummary Summary => Outcome.Summary;
        /// <summary>Gets a read-only snapshot of source and record diagnostics.</summary>
        public IReadOnlyList<ImportDiagnostic> Diagnostics => Outcome.Diagnostics;
        /// <summary>Gets a read-only snapshot of accepted record references; each record instance remains caller-owned.</summary>
        public IReadOnlyList<T> AcceptedRecords { get; private set; }
        /// <summary>Gets the supplied display message, which reporting adapters may replace with a safe fixed template.</summary>
        public string Message { get; private set; }
    }
}
