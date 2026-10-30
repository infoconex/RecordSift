using System;

namespace RecordSift
{
    /// <summary>Stable release 0.1 diagnostic identifiers and trusted default messages.</summary>
    public static class DiagnosticCodes
    {
        public const string SourceUnreadable = "SOURCE_UNREADABLE";
        public const string SourceLimitExceeded = "SOURCE_LIMIT_EXCEEDED";
        public const string RequiredHeaderMissing = "REQUIRED_HEADER_MISSING";
        public const string RequiredColumnMissing = "REQUIRED_COLUMN_MISSING";
        public const string AmbiguousColumn = "AMBIGUOUS_COLUMN";
        public const string InvalidColumnPosition = "INVALID_COLUMN_POSITION";
        public const string InvalidCsvDelimiter = "INVALID_CSV_DELIMITER";
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
            return code == SourceUnreadable || code == SourceLimitExceeded ||
                   code == RequiredHeaderMissing ||
                   code == MalformedSource || code == RequiredColumnMissing ||
                   code == AmbiguousColumn || code == InvalidColumnPosition ||
                   code == InvalidCsvDelimiter;
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
                case SourceUnreadable: return DomainMessageResources.Get(SourceUnreadable);
                case SourceLimitExceeded: return DomainMessageResources.Get(SourceLimitExceeded);
                case RequiredHeaderMissing: return DomainMessageResources.Get(RequiredHeaderMissing);
                case RequiredColumnMissing: return DomainMessageResources.Get(RequiredColumnMissing);
                case AmbiguousColumn: return DomainMessageResources.Get(AmbiguousColumn);
                case InvalidColumnPosition: return DomainMessageResources.Get(InvalidColumnPosition);
                case MalformedSource: return DomainMessageResources.Get(MalformedSource);
                case InvalidCsvDelimiter: return DomainMessageResources.Get(InvalidCsvDelimiter);
                case RequiredFieldMissing: return DomainMessageResources.Get(RequiredFieldMissing);
                case ConversionFailed: return DomainMessageResources.Get(ConversionFailed);
                case ValidationFailed: return DomainMessageResources.Get(ValidationFailed);
                default: throw new ArgumentException(DomainMessageResources.Get("Argument_UnknownDiagnosticCode"), nameof(code));
            }
        }
    }
}
