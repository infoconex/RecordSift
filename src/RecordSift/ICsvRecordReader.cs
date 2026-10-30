using System.IO;

namespace RecordSift
{
    /// <summary>
    /// RecordSift-owned input port for raw CSV records. Implementations must not
    /// dispose the caller's reader and must return no partial records on failure.
    /// </summary>
    public interface ICsvRecordReader
    {
        /// <summary>
        /// Reads already-decoded CSV text with an explicit positive character budget.
        /// Returns ordered records with one-based source-record and physical-line positions.
        /// </summary>
        CsvParseResult Read(TextReader source, int maxCharacters);
    }
}
