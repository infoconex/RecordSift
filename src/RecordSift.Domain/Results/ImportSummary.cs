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

        public int ProcessedCount { get; private set; }
        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
    }
}
