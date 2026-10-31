using System.IO;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>
    /// A replaceable workbook reader; the caller owns source. Parse failures are atomic
    /// and never include vendor exception details or source cell values.
    /// </summary>
    public interface IXlsxWorksheetReader
    {
        /// <summary>Reads only the selected worksheet (or the first if unnamed) without disposing the input stream. Invalid arguments throw; malformed, unreadable or over-budget sources return atomic safe diagnostics. Compressed input is also subject to adapter-level ZIP expansion and entry safeguards.</summary>
        /// <param name="source">Caller-owned XLSX byte stream; never disposed by the reader.</param>
        /// <param name="maxBytes">Positive limit on raw compressed input bytes.</param>
        /// <param name="maxRows">Positive maximum one-based worksheet row index examined, including headers.</param>
        /// <param name="maxColumns">Positive limit on columns in an examined row.</param>
        /// <param name="maxCellCharacters">Positive UTF-16 character limit on each decoded cell.</param>
        /// <param name="worksheetName">Optional exact-case worksheet selection; null selects first.</param>
        /// <returns>Read-only ordered nonblank source rows on success, or an empty failure result with a source diagnostic.</returns>
        XlsxReadResult Read(Stream source, int maxBytes,
            int maxRows, int maxColumns, int maxCellCharacters,
            string worksheetName = null);
    }
}
