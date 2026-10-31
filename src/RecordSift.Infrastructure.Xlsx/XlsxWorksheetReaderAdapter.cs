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
        // Release 0.1 defense-in-depth: cap ZIP entry count and expanded bytes in addition
        // to the caller's compressed byte limit; these budgets are not caller overrides.
        private const int MaximumPackageEntries = 512;
        private const int MaximumUncompressedBytes = 67108864;
        private const int BufferSize = 8192;

        /// <summary>Reads a selected XLSX worksheet using ExcelDataReader, first enforcing compressed-byte and ZIP expansion safeguards; failures are atomic and never expose cell values or vendor exceptions.</summary>
        /// <param name="source">Caller-owned TextReader or Stream, which remains open after this call.</param>
        /// <param name="maxBytes">Positive maximum compressed input bytes.</param>
        /// <param name="maxRows">Positive maximum physical worksheet row index.</param>
        /// <param name="maxColumns">Positive maximum decoded fields in any row.</param>
        /// <param name="maxCellCharacters">Positive maximum UTF-16 code units per decoded cell.</param>
        /// <param name="worksheetName">Optional exact-case worksheet name; null uses first available worksheet.</param>
        /// <remarks>Release 0.1 package preflight independently limits the archive to 512 ZIP entries and total declared expansion to the smaller of 64 MiB or 32 times the supplied compressed-byte budget. Entries containing macro binaries or external-link parts are rejected. These internal guards supplement the caller's row, column and cell-character budgets.</remarks>
        /// <remarks>Stored worksheet values are formatted using invariant culture; DateTime cell values use the round-trip ISO 8601 format. No formula or macro code is evaluated by this reader.</remarks>
        public XlsxReadResult Read(Stream source, int maxBytes,
            int maxRows, int maxColumns, int maxCellCharacters,
            string worksheetName = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            if (maxRows < 1) throw new ArgumentOutOfRangeException(nameof(maxRows));
            if (maxColumns < 1) throw new ArgumentOutOfRangeException(nameof(maxColumns));
            if (maxCellCharacters < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCellCharacters));
            if (worksheetName != null && string.IsNullOrWhiteSpace(worksheetName))
                throw new ArgumentException(DomainMessageResources.DiagnosticWorksheetBlank,
                    nameof(worksheetName));

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
                    var packageError = CheckPackage(buffer, maxBytes);
                    if (packageError != null)
                        return Error(packageError);

                    buffer.Position = 0;
                    using (var workbook = ExcelReaderFactory.CreateOpenXmlReader(buffer,
                        new ExcelReaderConfiguration { LeaveOpen = true }))
                    {
                        if (workbook.ResultsCount == 0)
                            return Error(DiagnosticCodes.MalformedSource);

                        if (worksheetName != null)
                        {
                            var selected = false;
                            do
                            {
                                if (string.Equals(workbook.Name, worksheetName,
                                    StringComparison.Ordinal))
                                {
                                    selected = true;
                                    break;
                                }
                            } while (workbook.NextResult());

                            if (!selected)
                                return Error(DiagnosticCodes.WorksheetNotFound);
                        }

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

        private static string CheckPackage(Stream stream, int maxBytes)
        {
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                if (zip.Entries.Count == 0)
                    return DiagnosticCodes.MalformedSource;
                if (zip.Entries.Count > MaximumPackageEntries)
                    return DiagnosticCodes.SourceLimitExceeded;
                long expanded = 0;
                var expandedLimit = Math.Min((long)MaximumUncompressedBytes, (long)maxBytes * 32);
                foreach (var entry in zip.Entries)
                {
                    if (entry.Length > expandedLimit - expanded)
                        return DiagnosticCodes.SourceLimitExceeded;
                    expanded += entry.Length;
                    var path = entry.FullName.Replace('\\', '/');
                    if (path.StartsWith("xl/externalLinks/", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                        return DiagnosticCodes.MalformedSource;
                }
                return zip.GetEntry("xl/workbook.xml") == null
                    ? DiagnosticCodes.MalformedSource : null;
            }
        }

        private static XlsxReadResult Error(string code)
        {
            return XlsxReadResult.Failure(ImportDiagnostic.ForSource(null, code));
        }
    }
}
