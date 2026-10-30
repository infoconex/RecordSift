using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;
using RecordSift.Domain.Results;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Identifies whether an error refers to a data row or the import source.</summary>
    public enum DiagnosticScope
    {
        Record,
        Source
    }
}
