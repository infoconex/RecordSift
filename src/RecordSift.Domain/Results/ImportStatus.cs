using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;
using RecordSift.Domain.Results;

namespace RecordSift.Domain.Results
{
    /// <summary>Machine-readable outcome of a source import.</summary>
    public enum ImportStatus
    {
        Completed,
        Failed
    }
}
