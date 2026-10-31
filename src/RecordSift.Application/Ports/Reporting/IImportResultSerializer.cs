using RecordSift.Domain.Results;

namespace RecordSift.Application.Ports.Reporting
{
    /// <summary>
    /// Serializes an existing import result without reopening its original source.
    /// Implementations own the output format and have no vendor dependencies in this contract.
    /// </summary>
    public interface IImportResultSerializer
    {
        /// <summary>Produces machine-readable output from existing result metadata without accessing source data or accepted CLR record values.</summary>
        /// <typeparam name="T">Accepted CLR record type.</typeparam>
        /// <param name="result">Existing typed outcome; null is invalid.</param>
        /// <returns>Serialized report using the adapter's defined wire contract.</returns>
        string Serialize<T>(TypedImportResult<T> result);
    }
}
