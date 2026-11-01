using System;
using System.Globalization;
using System.Text;
using RecordSift.Application.Ports.Reporting;
using RecordSift.Domain.Results;

namespace RecordSift.Infrastructure.Reporting
{
    /// <summary>
    /// Exports only existing structured diagnostic metadata as deterministic CSV.
    /// It never reads input data or exports accepted records or caller status messages.
    /// This is lossless machine-interchange CSV, not a spreadsheet-safe export:
    /// formula-like caller-controlled source, field and worksheet identifiers are
    /// deliberately preserved, not neutralized for spreadsheet evaluation.
    /// </summary>
    public sealed class CsvImportResultSerializerAdapter : IImportResultSerializer
    {
        private static readonly string[] s_headers = {
            "SourceId", "Scope", "Row", "Field", "Code", "Message",
            "Worksheet", "PhysicalRow", "PhysicalLine"
        };

        private readonly char _delimiter;

        /// <summary>Creates a CSV reporter with a comma delimiter.</summary>
        public CsvImportResultSerializerAdapter()
            : this(',')
        {
        }

        /// <summary>Creates a CSV reporter with the specified field delimiter.</summary>
        /// <param name="delimiter">A single delimiter other than quote, CR, LF or a non-tab control character.</param>
        public CsvImportResultSerializerAdapter(char delimiter)
        {
            if (delimiter == '"' || delimiter == '\r' || delimiter == '\n' ||
                (char.IsControl(delimiter) && delimiter != '\t'))
                throw new ArgumentException(ReportingMessageResources.CsvInvalidDelimiter,
                    nameof(delimiter));

            _delimiter = delimiter;
        }

        /// <summary>
        /// Returns a header followed by one row per diagnostic, in original order, with CRLF
        /// record endings. Empty diagnostics yield a header-only report. No source is reopened,
        /// and no accepted CLR values or caller-provided summary message are included.
        /// Encoding is the responsibility of the consumer writing this string to bytes.
        /// Spreadsheet programs may interpret formula-like metadata as formulas; RFC-style
        /// CSV quoting alone does not prevent this. Consumers must not open untrusted
        /// diagnostic CSV in a spreadsheet without separate spreadsheet-specific safeguards.
        /// </summary>
        /// <typeparam name="T">The type of accepted records; their values are never inspected.</typeparam>
        /// <param name="result">An existing immutable typed import result.</param>
        /// <returns>Deterministic escaped CSV text.</returns>
        public string Serialize<T>(TypedImportResult<T> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var builder = new StringBuilder();
            for (var index = 0; index < s_headers.Length; index++)
            {
                if (index != 0)
                    builder.Append(_delimiter);
                AppendField(builder, s_headers[index], _delimiter);
            }
            builder.Append("\r\n");

            var safeSourceId = SafeReportingSourceIdentifier.Sanitize(result.SourceId);
            foreach (var diagnostic in result.Diagnostics)
            {
                AppendField(builder, safeSourceId, _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, diagnostic.Scope.ToString(), _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, NullableInt(diagnostic.Row), _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, diagnostic.Field, _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, diagnostic.Code, _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, diagnostic.Message, _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, diagnostic.Worksheet, _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, NullableInt(diagnostic.PhysicalRow), _delimiter);
                builder.Append(_delimiter);
                AppendField(builder, NullableInt(diagnostic.PhysicalLine), _delimiter);
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        private static string NullableInt(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null;
        }

        private static void AppendField(StringBuilder builder, string value, char delimiter)
        {
            if (value == null)
                return;

            var quote = value.IndexOf(delimiter) >= 0 || value.IndexOf('"') >= 0 ||
                value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0;
            if (!quote)
            {
                builder.Append(value);
                return;
            }

            builder.Append('"');
            foreach (var character in value)
            {
                if (character == '"')
                    builder.Append('"');
                builder.Append(character);
            }
            builder.Append('"');
        }
    }
}
