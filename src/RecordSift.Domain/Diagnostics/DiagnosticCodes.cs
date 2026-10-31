using System;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Stable release 0.1 diagnostic identifiers and trusted default messages.</summary>
    public static class DiagnosticCodes
    {
        public const string SourceUnreadable = "SOURCE_UNREADABLE";
        public const string SourceDecodingFailed = "SOURCE_DECODING_FAILED";
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
        public const string FieldLengthOutOfRange = "FIELD_LENGTH_OUT_OF_RANGE";
        public const string FieldRangeOutOfRange = "FIELD_RANGE_OUT_OF_RANGE";
        public const string DuplicateIdentifier = "DUPLICATE_IDENTIFIER";

        public static bool IsKnown(string code)
        {
            return IsSourceCode(code) || IsRecordCode(code);
        }

        public static bool IsSourceCode(string code)
        {
            return code == SourceUnreadable || code == SourceDecodingFailed ||
                   code == SourceLimitExceeded ||
                   code == RequiredHeaderMissing ||
                   code == MalformedSource || code == RequiredColumnMissing ||
                   code == AmbiguousColumn || code == InvalidColumnPosition ||
                   code == InvalidCsvDelimiter;
        }

        public static bool IsRecordCode(string code)
        {
            return code == RequiredFieldMissing || code == ConversionFailed ||
                   code == ValidationFailed || code == FieldLengthOutOfRange ||
                   code == FieldRangeOutOfRange || code == DuplicateIdentifier;
        }

        internal static string GetSafeMessage(string code)
        {
            switch (code)
            {
                case SourceUnreadable: return DomainMessageResources.Get(SourceUnreadable);
                case SourceDecodingFailed: return DomainMessageResources.Get(SourceDecodingFailed);
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
                case FieldLengthOutOfRange: return DomainMessageResources.Get(FieldLengthOutOfRange);
                case FieldRangeOutOfRange: return DomainMessageResources.Get(FieldRangeOutOfRange);
                case DuplicateIdentifier: return DomainMessageResources.Get(DuplicateIdentifier);
                default: throw new ArgumentException(DomainMessageResources.UnknownDiagnosticCode, nameof(code));
            }
        }
    }
}
