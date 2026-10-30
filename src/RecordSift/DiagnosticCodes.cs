using System;

namespace RecordSift
{
    /// <summary>Stable release 0.1 diagnostic identifiers and trusted default messages.</summary>
    public static class DiagnosticCodes
    {
        public const string SourceUnreadable = "SOURCE_UNREADABLE";
        public const string RequiredHeaderMissing = "REQUIRED_HEADER_MISSING";
        public const string RequiredColumnMissing = "REQUIRED_COLUMN_MISSING";
        public const string AmbiguousColumn = "AMBIGUOUS_COLUMN";
        public const string MalformedSource = "MALFORMED_SOURCE";
        public const string RequiredFieldMissing = "REQUIRED_FIELD_MISSING";
        public const string ConversionFailed = "CONVERSION_FAILED";
        public const string ValidationFailed = "VALIDATION_FAILED";

        public static bool IsKnown(string code)
        {
            return IsSourceCode(code) || IsRecordCode(code);
        }

        public static bool IsSourceCode(string code)
        {
            return code == SourceUnreadable || code == RequiredHeaderMissing ||
                   code == MalformedSource || code == RequiredColumnMissing ||
                   code == AmbiguousColumn;
        }

        public static bool IsRecordCode(string code)
        {
            return code == RequiredFieldMissing || code == ConversionFailed ||
                   code == ValidationFailed;
        }

        internal static string GetSafeMessage(string code)
        {
            switch (code)
            {
                case SourceUnreadable: return "The source could not be read.";
                case RequiredHeaderMissing: return "A required source header is missing.";
                case RequiredColumnMissing: return "A required source column is missing.";
                case AmbiguousColumn: return "A declared source column is ambiguous.";
                case MalformedSource: return "The source structure is malformed.";
                case RequiredFieldMissing: return "A required field is missing.";
                case ConversionFailed: return "A field could not be converted.";
                case ValidationFailed: return "A field failed validation.";
                default: throw new ArgumentException("Unknown diagnostic code.", nameof(code));
            }
        }
    }
}
