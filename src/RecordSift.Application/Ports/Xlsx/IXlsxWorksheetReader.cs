using System.IO;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>
    /// A replaceable workbook reader; the caller owns source. Parse failures are atomic
    /// and never include vendor exception details or source cell values.
    /// </summary>
    public interface IXlsxWorksheetReader
    {
        XlsxReadResult Read(Stream source, int maxBytes,
            int maxRows, int maxColumns, int maxCellCharacters);
    }
}
