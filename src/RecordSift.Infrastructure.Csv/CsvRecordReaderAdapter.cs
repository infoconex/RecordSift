using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Infrastructure.Csv
{
    /// <summary>
    /// CsvHelper-backed implementation of the RecordSift-owned CSV record port.
    /// Parser types and exceptions never cross this adapter boundary.
    /// </summary>
    public sealed class CsvRecordReaderAdapter : ICsvRecordReader
    {
        public CsvParseResult Read(TextReader source, int maxCharacters, CsvReadOptions options = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (maxCharacters <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCharacters));

            if (options != null && !options.IsValid)
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.InvalidCsvDelimiter));

            var records = new List<CsvRecord>();
            var physicalLine = 1;
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = (options ?? CsvReadOptions.Default).Delimiter,
                HasHeaderRecord = false,
                IgnoreBlankLines = false,
                ExceptionMessagesContainRawData = false,
                BadDataFound = args =>
                {
                    // CsvHelper identifies the malformed raw field; count physical line
                    // breaks within that field to locate its opening line without parsing CSV.
                    var currentLine = args.Context.Parser.RawRow;
                    var fieldLines = CountLineBreaks(args.Field);
                    throw new MalformedCsvException(Math.Max(1, currentLine - fieldLines));
                }
            };

            try
            {
                using (var limited = new CharacterLimitedTextReader(source, maxCharacters))
                using (var parser = new CsvParser(limited, configuration, true))
                {
                    while (parser.Read())
                    {
                        records.Add(new CsvRecord(records.Count + 1, physicalLine, parser.Record));
                        physicalLine = parser.RawRow + 1;
                    }
                }

                return CsvParseResult.Success(records);
            }
            catch (MalformedCsvException error)
            {
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.MalformedSource, null, null, error.PhysicalLine));
            }
            catch (CharacterLimitExceededException)
            {
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.SourceLimitExceeded));
            }
            catch (CsvHelperException)
            {
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.MalformedSource, null, null, physicalLine));
            }
            catch (IOException)
            {
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.SourceUnreadable));
            }
            catch (ObjectDisposedException)
            {
                return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.SourceUnreadable));
            }
        }

        private static int CountLineBreaks(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var lines = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\r')
                {
                    lines++;
                    if (i + 1 < value.Length && value[i + 1] == '\n')
                        i++;
                }
                else if (value[i] == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }
    }

}
