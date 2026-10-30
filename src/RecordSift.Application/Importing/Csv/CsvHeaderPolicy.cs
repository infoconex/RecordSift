using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;
using RecordSift.Domain.Results;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Application.Importing.Csv;

namespace RecordSift.Application.Importing.Csv
{
    /// <summary>Explicit CSV header policy. No automatic header inference.</summary>
    public enum CsvHeaderPolicy
    {
        WithHeader = 1,
        WithoutHeader = 2
    }
}
