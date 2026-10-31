using System;

namespace RecordSift.Domain.Results
{
    /// <summary>Immutable counts of distinct logical data records, excluding headers.</summary>
    public sealed class ImportSummary
    {
        internal ImportSummary(int processedCount, int rejectedCount)
        {
            if (processedCount < 0) throw new ArgumentOutOfRangeException(nameof(processedCount));
            if (rejectedCount < 0 || rejectedCount > processedCount)
                throw new ArgumentOutOfRangeException(nameof(rejectedCount));

            ProcessedCount = processedCount;
            RejectedCount = rejectedCount;
            AcceptedCount = processedCount - rejectedCount;
        }

        /// <summary>Gets the number of logical data records processed, excluding source headers.</summary>
        public int ProcessedCount { get; private set; }
        /// <summary>Gets processed records with no record-level errors.</summary>
        public int AcceptedCount { get; private set; }
        /// <summary>Gets distinct logical records rejected, regardless of the number of errors per record.</summary>
        public int RejectedCount { get; private set; }
    }
}
