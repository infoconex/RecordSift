using System;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Stable release 0.1 diagnostic identifiers and trusted default messages.</summary>
    public static class DiagnosticCodes
    {
        public static readonly string SourceUnreadable = "SOURCE_UNREADABLE";
        public static readonly string SourceDecodingFailed = "SOURCE_DECODING_FAILED";
        public static readonly string SourceLimitExceeded = "SOURCE_LIMIT_EXCEEDED";
        public static readonly string RequiredHeaderMissing = "REQUIRED_HEADER_MISSING";
        public static readonly string RequiredColumnMissing = "REQUIRED_COLUMN_MISSING";
        public static readonly string AmbiguousColumn = "AMBIGUOUS_COLUMN";
        public static readonly string InvalidColumnPosition = "INVALID_COLUMN_POSITION";
        public static readonly string InvalidCsvDelimiter = "INVALID_CSV_DELIMITER";
        public static readonly string MalformedSource = "MALFORMED_SOURCE";
        public static readonly string WorksheetNotFound = "WORKSHEET_NOT_FOUND";
        public static readonly string RequiredFieldMissing = "REQUIRED_FIELD_MISSING";
        public static readonly string ConversionFailed = "CONVERSION_FAILED";
        public static readonly string ValidationFailed = "VALIDATION_FAILED";
        public static readonly string FieldLengthOutOfRange = "FIELD_LENGTH_OUT_OF_RANGE";
        public static readonly string FieldRangeOutOfRange = "FIELD_RANGE_OUT_OF_RANGE";
        public static readonly string DuplicateIdentifier = "DUPLICATE_IDENTIFIER";

        internal static bool IsKnown(string code)
        {
            return IsSourceCode(code) || IsRecordCode(code);
        }

        internal static bool IsSourceCode(string code)
        {
            return code == SourceUnreadable || code == SourceDecodingFailed ||
                   code == SourceLimitExceeded ||
                   code == RequiredHeaderMissing ||
                   code == MalformedSource || code == WorksheetNotFound ||
                   code == RequiredColumnMissing ||
                   code == AmbiguousColumn || code == InvalidColumnPosition ||
                   code == InvalidCsvDelimiter;
        }

        internal static bool IsRecordCode(string code)
        {
            return code == RequiredFieldMissing || code == ConversionFailed ||
                   code == ValidationFailed || code == FieldLengthOutOfRange ||
                   code == FieldRangeOutOfRange || code == DuplicateIdentifier;
        }

        internal static string GetSafeMessage(string code)
        {
            if (!IsKnown(code))
                throw new ArgumentException(DomainMessageResources.UnknownDiagnosticCode,
                    nameof(code));

            return DomainMessageResources.Get(code);
        }
    }
}
