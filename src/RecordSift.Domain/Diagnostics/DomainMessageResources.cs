using System.Globalization;
using System.Resources;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>
    /// Core-owned resource catalog: stable machine codes and invariant messages.
    /// The adapter never owns these descriptions or exposes vendor exception text.
    /// </summary>
    public static class DomainMessageResources
    {
        private static readonly ResourceManager s_catalog = new ResourceManager(
            "RecordSift.Resources.DomainMessages", typeof(DomainMessageResources).Assembly);

        public static string ConversionInvalidValue => Get("Argument_ConversionInvalidValue");
        public static string ConversionUnsupportedType => Get("Argument_ConversionUnsupportedType");
        public static string PropertyMappingUnsupportedMember => Get("Argument_PropertyMappingUnsupportedMember");
        public static string PropertyMappingRequiredSourceMissing => Get("Argument_PropertyMappingRequiredSourceMissing");
        public static string PropertyMappingSourceName => Get("Argument_PropertyMappingSourceName");
        public static string PropertyMappingDestinationName => Get("Argument_PropertyMappingDestinationName");
        public static string PropertyMappingNull => Get("Argument_PropertyMappingNull");
        public static string PropertyMappingUndeclaredSource => Get("Argument_PropertyMappingUndeclaredSource");
        public static string PropertyMappingDuplicateDestination => Get("Argument_PropertyMappingDuplicateDestination");
        public static string PropertyMappingEmpty => Get("Argument_PropertyMappingEmpty");
        public static string PropertyMappingUnsupportedDestination => Get("Argument_PropertyMappingUnsupportedDestination");
        public static string AcceptedRecordCountMismatch => Get("Argument_AcceptedRecordCountMismatch");
        public static string ImportSourceIdBlank => Get("Argument_ImportSourceIdBlank");
        public static string ImportMessageBlank => Get("Argument_ImportMessageBlank");
        public static string BoundColumnName => Get("Argument_BoundColumnName");
        public static string ColumnDeclarationNull => Get("Argument_ColumnDeclarationNull");
        public static string ColumnNamesDuplicate => Get("Argument_ColumnNamesDuplicate");
        public static string CompletedSourceError => Get("Argument_CompletedSourceError");
        public static string DiagnosticCodeNotRecord => Get("Argument_DiagnosticCodeNotRecord");
        public static string DiagnosticCodeNotSource => Get("Argument_DiagnosticCodeNotSource");
        public static string DiagnosticFieldBlank => Get("Argument_DiagnosticFieldBlank");
        public static string DiagnosticNull => Get("Argument_DiagnosticNull");
        public static string DiagnosticRowOutOfRange => Get("Argument_DiagnosticRowOutOfRange");
        public static string DiagnosticScopeUnknown => Get("Argument_DiagnosticScopeUnknown");
        public static string DiagnosticWorksheetBlank => Get("Argument_DiagnosticWorksheetBlank");
        public static string ExpectedColumnName => Get("Argument_ExpectedColumnName");
        public static string FieldLengthMinimumExceedsMaximum => Get("Argument_FieldLengthMinimumExceedsMaximum");
        public static string RangeBoundsInvalid => Get("Argument_RangeBoundsInvalid");
        public static string RangeFieldName => Get("Argument_RangeFieldName");
        public static string RangeRuleNull => Get("Argument_RangeRuleNull");
        public static string DuplicateFieldName => Get("Argument_DuplicateFieldName");
        public static string FailedWithoutSourceError => Get("Argument_FailedWithoutSourceError");
        public static string PositionMappingDuplicate => Get("Argument_PositionMappingDuplicate");
        public static string PositionMappingNull => Get("Argument_PositionMappingNull");
        public static string PositionMappingUndeclared => Get("Argument_PositionMappingUndeclared");
        public static string SourceDiagnosticHasRow => Get("Argument_SourceDiagnosticHasRow");
        public static string SourcePositionName => Get("Argument_SourcePositionName");
        public static string UnknownDiagnosticCode => Get("Argument_UnknownDiagnosticCode");

        internal static string Get(string resourceKey)
        {
            // Fixed invariant-culture report messages are part of release 0.1's
            // observable contract. Presentation localization can be added separately.
            string message = s_catalog.GetString(resourceKey, CultureInfo.InvariantCulture);
            if (message == null)
                throw new MissingManifestResourceException();
            return message;
        }
    }
}
