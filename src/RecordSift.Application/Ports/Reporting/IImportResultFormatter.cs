using RecordSift.Domain.Results;

namespace RecordSift.Application.Ports.Reporting
{
    /// <summary>
    /// Renders an existing import result for people while preserving its
    /// independent machine-readable status and diagnostic contracts.
    /// </summary>
    public interface IImportResultFormatter
    {
        /// <summary>Renders an in-memory result into a safe human-readable summary without reopening input or exporting accepted record values.</summary>
        /// <typeparam name="T">Accepted CLR record type.</typeparam>
        /// <param name="result">Existing typed outcome; null is invalid.</param>
        /// <returns>Invariant-readable counts and registered diagnostic metadata.</returns>
        string Format<T>(TypedImportResult<T> result);
    }
}
