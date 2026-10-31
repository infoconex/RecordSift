using RecordSift.Domain.Results;

namespace RecordSift.Application.Ports.Reporting
{
    /// <summary>
    /// Serializes an existing import result without reopening its original source.
    /// Implementations own the output format and have no vendor dependencies in this contract.
    /// </summary>
    public interface IImportResultSerializer
    {
        string Serialize<T>(TypedImportResult<T> result);
    }
}
