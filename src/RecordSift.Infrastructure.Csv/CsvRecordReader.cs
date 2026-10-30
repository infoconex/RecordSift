using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace RecordSift.Infrastructure.Csv
{
    /// <summary>
    /// CsvHelper-backed implementation of the RecordSift-owned CSV record port.
    /// Parser types and exceptions never cross this adapter boundary.
    /// </summary>
    public sealed class CsvRecordReader : ICsvRecordReader
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

    /// <summary>
    /// Counts decoded characters only; this wrapper owns neither the caller's reader
    /// nor CSV grammar. It permits an exact-budget EOF and detects a nonempty overrun.
    /// </summary>
    internal sealed class CharacterLimitedTextReader : TextReader
    {
        private readonly TextReader _source;
        private int _remaining;

        internal CharacterLimitedTextReader(TextReader source, int maxCharacters)
        {
            _source = source;
            _remaining = maxCharacters;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (count == 0)
                return 0;
            // Probe at most one character beyond the accepted budget.
            var requested = Math.Min(count, _remaining + 1);
            var read = _source.Read(buffer, index, requested);
            if (read > _remaining)
                throw new CharacterLimitExceededException();
            _remaining -= read;
            return read;
        }

        public override int Read()
        {
            var value = _source.Read();
            if (value < 0)
                return value;
            if (_remaining == 0)
                throw new CharacterLimitExceededException();
            _remaining--;
            return value;
        }

        protected override void Dispose(bool disposing)
        {
            // Explicitly do not dispose caller-owned TextReader.
            base.Dispose(disposing);
        }
    }

    internal sealed class MalformedCsvException : Exception
    {
        internal MalformedCsvException(int physicalLine)
        {
            PhysicalLine = physicalLine;
        }

        internal int PhysicalLine { get; private set; }
    }

    internal sealed class CharacterLimitExceededException : Exception
    {
    }
}
