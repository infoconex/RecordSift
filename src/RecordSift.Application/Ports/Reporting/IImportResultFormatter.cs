using RecordSift.Domain.Results;

namespace RecordSift.Application.Ports.Reporting
{
    /// <summary>
    /// Renders an existing import result for people while preserving its
    /// independent machine-readable status and diagnostic contracts.
    /// </summary>
    public interface IImportResultFormatter
    {
        string Format<T>(TypedImportResult<T> result);
    }
}
