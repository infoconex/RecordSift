using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using ExcelDataReader;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Infrastructure.Xlsx
{
    /// <summary>ExcelDataReader-backed XLSX-only reader; never returns vendor types.</summary>
    public sealed class XlsxWorksheetReaderAdapter : IXlsxWorksheetReader
    {
        private const int MaximumPackageEntries = 512;
        private const int MaximumUncompressedBytes = 67108864;
        private const int BufferSize = 8192;

        public XlsxReadResult Read(Stream source, int maxBytes,
            int maxRows, int maxColumns, int maxCellCharacters)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            if (maxRows < 1) throw new ArgumentOutOfRangeException(nameof(maxRows));
            if (maxColumns < 1) throw new ArgumentOutOfRangeException(nameof(maxColumns));
            if (maxCellCharacters < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCellCharacters));

            try
            {
                using (var buffer = new MemoryStream())
                {
                    var bytes = new byte[BufferSize];
                    int count;
                    while ((count = source.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        if (count > maxBytes - buffer.Length)
                            return Error(DiagnosticCodes.SourceLimitExceeded);
                        buffer.Write(bytes, 0, count);
                    }

                    buffer.Position = 0;
                    if (!IsSafePackage(buffer, maxBytes))
                        return Error(DiagnosticCodes.SourceLimitExceeded);

                    buffer.Position = 0;
                    using (var workbook = ExcelReaderFactory.CreateOpenXmlReader(buffer,
                        new ExcelReaderConfiguration { LeaveOpen = true }))
                    {
                        if (workbook.ResultsCount == 0)
                            return Error(DiagnosticCodes.MalformedSource);

                        var worksheet = workbook.Name;
                        var records = new List<XlsxRecord>();
                        var totalCells = 0;
                        while (workbook.Read())
                        {
                            if (workbook.Depth + 1 > maxRows || workbook.FieldCount > maxColumns ||
                                workbook.FieldCount > maxRows * (long)maxColumns - totalCells)
                                return Error(DiagnosticCodes.SourceLimitExceeded);

                            totalCells += workbook.FieldCount;
                            var values = new List<string>();
                            var hasContent = false;
                            for (var i = 0; i < workbook.FieldCount; i++)
                            {
                                var value = workbook.GetValue(i);
                                var cell = value is DateTime
                                    ? ((DateTime)value).ToString("o", CultureInfo.InvariantCulture)
                                    : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                                if (cell.Length > maxCellCharacters)
                                    return Error(DiagnosticCodes.SourceLimitExceeded);
                                values.Add(cell);
                                if (!string.IsNullOrWhiteSpace(cell)) hasContent = true;
                            }
                            if (hasContent)
                                records.Add(new XlsxRecord(workbook.Depth + 1, values));
                        }
                        return XlsxReadResult.Success(worksheet, records);
                    }
                }
            }
            catch (IOException)
            {
                return Error(DiagnosticCodes.SourceUnreadable);
            }
            catch (UnauthorizedAccessException)
            {
                return Error(DiagnosticCodes.SourceUnreadable);
            }
            catch (Exception error) when (!(error is OutOfMemoryException) &&
                !(error is StackOverflowException) && !(error is System.Threading.ThreadAbortException))
            {
                // Do not pass parser exceptions, paths or untrusted worksheet values outward.
                return Error(DiagnosticCodes.MalformedSource);
            }
        }

        private static bool IsSafePackage(Stream stream, int maxBytes)
        {
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                if (zip.Entries.Count == 0 || zip.Entries.Count > MaximumPackageEntries)
                    return false;
                long expanded = 0;
                var expandedLimit = Math.Min((long)MaximumUncompressedBytes, (long)maxBytes * 32);
                foreach (var entry in zip.Entries)
                {
                    if (entry.Length > expandedLimit - expanded)
                        return false;
                    expanded += entry.Length;
                    var path = entry.FullName.Replace('\\', '/');
                    if (path.StartsWith("xl/externalLinks/", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return zip.GetEntry("xl/workbook.xml") != null;
            }
        }

        private static XlsxReadResult Error(string code)
        {
            return XlsxReadResult.Failure(ImportDiagnostic.ForSource(null, code));
        }
    }
}
