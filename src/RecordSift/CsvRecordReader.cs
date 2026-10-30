using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RecordSift
{
    /// <summary>
    /// Minimal strict comma-separated logical-record parser for already-decoded text.
    /// This component does not infer headers, bind schemas, decode bytes or map CLR types.
    /// Source decoding, delimiters and header policy are separate requirements.
    /// </summary>
    public static class CsvRecordReader
    {
        private enum FieldState { Start, Unquoted, Quoted, AfterQuote }

        /// <summary>
        /// Parse raw source rows with an explicit positive character budget. Does not
        /// dispose the supplied reader. A source failure returns no partial records.
        /// </summary>
        public static CsvParseResult Read(TextReader source, int maxCharacters)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(maxCharacters));

            var records = new List<CsvRecord>();
            var fields = new List<string>();
            var field = new StringBuilder();
            var state = FieldState.Start;
            var charactersRead = 0;
            var physicalLine = 1;
            var recordStartLine = 1;
            var openQuoteLine = 1;
            var priorWasCarriageReturn = false;

            try
            {
                int next;
                while ((next = source.Read()) != -1)
                {
                    if (charactersRead == maxCharacters)
                        return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                            DiagnosticCodes.SourceLimitExceeded, null, null, physicalLine));
                    charactersRead++;

                    var c = (char)next;
                    var isCrLfTail = c == '\n' && priorWasCarriageReturn;
                    if (c == '\r')
                        physicalLine++;
                    else if (c == '\n' && !isCrLfTail)
                        physicalLine++;
                    priorWasCarriageReturn = c == '\r';

                    // A CRLF pair outside quoted content is one terminator, not two.
                    if (isCrLfTail && state != FieldState.Quoted)
                        continue;

                    var isNewline = c == '\r' || c == '\n';
                    if (state == FieldState.Start)
                    {
                        if (c == '"')
                        {
                            openQuoteLine = physicalLine;
                            state = FieldState.Quoted;
                        }
                        else if (c == ',')
                        {
                            fields.Add(string.Empty);
                        }
                        else if (isNewline)
                        {
                            AddRecord(records, fields, field, recordStartLine);
                            recordStartLine = physicalLine;
                        }
                        else
                        {
                            field.Append(c);
                            state = FieldState.Unquoted;
                        }
                    }
                    else if (state == FieldState.Unquoted)
                    {
                        if (c == '"')
                            return Malformed(physicalLine);
                        if (c == ',')
                        {
                            fields.Add(field.ToString());
                            field.Clear();
                            state = FieldState.Start;
                        }
                        else if (isNewline)
                        {
                            AddRecord(records, fields, field, recordStartLine);
                            state = FieldState.Start;
                            recordStartLine = physicalLine;
                        }
                        else
                            field.Append(c);
                    }
                    else if (state == FieldState.Quoted)
                    {
                        if (c == '"')
                            state = FieldState.AfterQuote;
                        else
                            field.Append(c); // Retain exact CRLF and delimiters in quoted field.
                    }
                    else // AfterQuote
                    {
                        if (c == '"')
                        {
                            field.Append('"');
                            state = FieldState.Quoted;
                        }
                        else if (c == ',')
                        {
                            fields.Add(field.ToString());
                            field.Clear();
                            state = FieldState.Start;
                        }
                        else if (isNewline)
                        {
                            AddRecord(records, fields, field, recordStartLine);
                            state = FieldState.Start;
                            recordStartLine = physicalLine;
                        }
                        else
                            return Malformed(physicalLine);
                    }
                }
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

            if (state == FieldState.Quoted)
                return Malformed(openQuoteLine);

            // A trailing delimiter produces an empty last field; a trailing record
            // terminator does not create a phantom record.
            if (state != FieldState.Start || fields.Count > 0)
                AddRecord(records, fields, field, recordStartLine);

            return CsvParseResult.Success(records);
        }

        private static CsvParseResult Malformed(int physicalLine)
        {
            return CsvParseResult.Failure(ImportDiagnostic.ForSource(null,
                DiagnosticCodes.MalformedSource, null, null, physicalLine));
        }

        private static void AddRecord(List<CsvRecord> records, List<string> fields,
            StringBuilder field, int physicalLine)
        {
            fields.Add(field.ToString());
            records.Add(new CsvRecord(records.Count + 1, physicalLine, fields));
            fields.Clear();
            field.Clear();
        }
    }
}
