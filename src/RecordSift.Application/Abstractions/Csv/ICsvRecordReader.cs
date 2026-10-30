using System.IO;

namespace RecordSift.Application.Abstractions.Csv
{
    /// <summary>
    /// RecordSift-owned input port: all CSV adapters implement this exact contract,
    /// independent of parsing vendor, version, or transport. Implementations must not
    /// dispose the caller's reader and must return no partial records on failure.
    /// </summary>
    public interface ICsvRecordReader
    {
        /// <summary>
        /// Reads already-decoded CSV text with an explicit positive character budget.
        /// Returns ordered source records including a possible header as first record.
        /// Each record has ordered fields, a one-based source-record ordinal, and the
        /// first one-based physical source line (including quoted multiline fields).
        /// Quotes escape doubled quotes and protect delimiters and physical newlines.
        /// A null options value selects the core-owned default comma delimiter.
        /// Invalid options produce an InvalidCsvDelimiter source-scoped diagnostic
        /// before reading input. Malformed input, read faults and character limit
        /// violations return safe, source-scoped registered diagnostics, no partial
        /// records, and do not expose raw values, paths or vendor exception text.
        /// A zero-character source returns a successful empty record collection.
        /// Caller retains ownership and disposal of source; result snapshots are
        /// immutable. No ambient current culture affects delimiter or error messages.
        /// </summary>
        CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null);
    }
}
