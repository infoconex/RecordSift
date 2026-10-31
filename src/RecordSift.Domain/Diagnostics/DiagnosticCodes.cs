using System;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Stable release 0.1 diagnostic identifiers and trusted default messages.</summary>
    public static class DiagnosticCodes
    {
        /// <summary>Stable source-scope diagnostic identifier <c>SOURCE_UNREADABLE</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string SourceUnreadable = "SOURCE_UNREADABLE";
        /// <summary>Stable source-scope diagnostic identifier <c>SOURCE_DECODING_FAILED</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string SourceDecodingFailed = "SOURCE_DECODING_FAILED";
        /// <summary>Stable source-scope diagnostic identifier <c>SOURCE_LIMIT_EXCEEDED</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string SourceLimitExceeded = "SOURCE_LIMIT_EXCEEDED";
        /// <summary>Stable source-scope diagnostic identifier <c>REQUIRED_HEADER_MISSING</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string RequiredHeaderMissing = "REQUIRED_HEADER_MISSING";
        /// <summary>Stable source-scope diagnostic identifier <c>REQUIRED_COLUMN_MISSING</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string RequiredColumnMissing = "REQUIRED_COLUMN_MISSING";
        /// <summary>Stable source-scope diagnostic identifier <c>AMBIGUOUS_COLUMN</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string AmbiguousColumn = "AMBIGUOUS_COLUMN";
        /// <summary>Stable source-scope diagnostic identifier <c>INVALID_COLUMN_POSITION</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string InvalidColumnPosition = "INVALID_COLUMN_POSITION";
        /// <summary>Stable source-scope diagnostic identifier <c>INVALID_CSV_DELIMITER</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string InvalidCsvDelimiter = "INVALID_CSV_DELIMITER";
        /// <summary>Stable source-scope diagnostic identifier <c>MALFORMED_SOURCE</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string MalformedSource = "MALFORMED_SOURCE";
        /// <summary>Stable source-scope diagnostic identifier <c>WORKSHEET_NOT_FOUND</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string WorksheetNotFound = "WORKSHEET_NOT_FOUND";
        /// <summary>Stable record-scope diagnostic identifier <c>REQUIRED_FIELD_MISSING</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string RequiredFieldMissing = "REQUIRED_FIELD_MISSING";
        /// <summary>Stable record-scope diagnostic identifier <c>CONVERSION_FAILED</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string ConversionFailed = "CONVERSION_FAILED";
        /// <summary>Stable record-scope diagnostic identifier <c>VALIDATION_FAILED</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string ValidationFailed = "VALIDATION_FAILED";
        /// <summary>Stable record-scope diagnostic identifier <c>FIELD_LENGTH_OUT_OF_RANGE</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string FieldLengthOutOfRange = "FIELD_LENGTH_OUT_OF_RANGE";
        /// <summary>Stable record-scope diagnostic identifier <c>FIELD_RANGE_OUT_OF_RANGE</c>; preserved for public API and JSON compatibility.</summary>
        public static readonly string FieldRangeOutOfRange = "FIELD_RANGE_OUT_OF_RANGE";
        /// <summary>Stable record-scope diagnostic identifier <c>DUPLICATE_IDENTIFIER</c>; preserved for public API and JSON compatibility.</summary>
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
