namespace RecordSift.Domain.Results
{
    /// <summary>Machine-readable outcome of a source import.</summary>
    public enum ImportStatus
    {
        /// <summary>The source was processed to completion, even when some records were rejected.</summary>
        Completed,
        /// <summary>A source or schema failure prevented successful completion.</summary>
        Failed,
        /// <summary>Validation stopped early at a record error under an explicitly selected fail-fast policy.</summary>
        Stopped,
        /// <summary>The caller requested cooperative cancellation; only the processed prefix is included.</summary>
        Canceled
    }
}
