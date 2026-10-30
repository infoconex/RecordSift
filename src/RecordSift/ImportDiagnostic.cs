using System;
using System.Text.RegularExpressions;

namespace RecordSift
{
    /// <summary>
    /// Immutable, supplied error information. Error detection, classification and
    /// message sanitization are the responsibility of their owning requirements (FR-028).
    /// </summary>
    public sealed class ImportDiagnostic
    {
        private static readonly Regex CodePattern =
            new Regex(@"^[A-Z][A-Z0-9_]*$", RegexOptions.CultureInvariant);

        private ImportDiagnostic(DiagnosticScope scope, int? row, string field,
            string code, string message)
        {
            if (field != null && string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field must be nonempty when supplied.", nameof(field));
            if (string.IsNullOrEmpty(code) || !CodePattern.IsMatch(code))
                throw new ArgumentException("Code must be an uppercase diagnostic identifier.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message must be nonempty.", nameof(message));

            Scope = scope;
            Row = row;
            Field = field;
            Code = code;
            Message = message;
        }

        public DiagnosticScope Scope { get; private set; }
        public int? Row { get; private set; }
        public string Field { get; private set; }
        public string Code { get; private set; }
        public string Message { get; private set; }

        /// <summary>Create a supplied error associated with a one-based logical data row.</summary>
        public static ImportDiagnostic ForRecord(int row, string field, string code, string message)
        {
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            return new ImportDiagnostic(DiagnosticScope.Record, row, field, code, message);
        }

        /// <summary>Create a supplied source/schema error that does not reject a data row.</summary>
        public static ImportDiagnostic ForSource(string field, string code, string message)
        {
            return new ImportDiagnostic(DiagnosticScope.Source, null, field, code, message);
        }
    }
}
