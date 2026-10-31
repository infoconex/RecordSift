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

        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ConversionInvalidValue => Get("Argument_ConversionInvalidValue");
        /// <summary>Gets the invariant, resource-backed converter rule validation message.</summary>
        public static string ConversionRuleNull => Get("Argument_ConversionRuleNull");
        /// <summary>Gets the invariant, resource-backed converter rule validation message.</summary>
        public static string ConversionRuleDuplicate => Get("Argument_ConversionRuleDuplicate");
        /// <summary>Gets the invariant, resource-backed converter rule validation message.</summary>
        public static string ConversionRuleUndeclared => Get("Argument_ConversionRuleUndeclared");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ConversionUnsupportedType => Get("Argument_ConversionUnsupportedType");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingUnsupportedMember => Get("Argument_PropertyMappingUnsupportedMember");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingRequiredSourceMissing => Get("Argument_PropertyMappingRequiredSourceMissing");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingSourceName => Get("Argument_PropertyMappingSourceName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingDestinationName => Get("Argument_PropertyMappingDestinationName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingNull => Get("Argument_PropertyMappingNull");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingUndeclaredSource => Get("Argument_PropertyMappingUndeclaredSource");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingDuplicateDestination => Get("Argument_PropertyMappingDuplicateDestination");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingEmpty => Get("Argument_PropertyMappingEmpty");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PropertyMappingUnsupportedDestination => Get("Argument_PropertyMappingUnsupportedDestination");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string AcceptedRecordCountMismatch => Get("Argument_AcceptedRecordCountMismatch");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ImportSourceIdBlank => Get("Argument_ImportSourceIdBlank");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ImportMessageBlank => Get("Argument_ImportMessageBlank");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string BoundColumnName => Get("Argument_BoundColumnName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ColumnDeclarationNull => Get("Argument_ColumnDeclarationNull");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ColumnNamesDuplicate => Get("Argument_ColumnNamesDuplicate");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string CompletedSourceError => Get("Argument_CompletedSourceError");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticCodeNotRecord => Get("Argument_DiagnosticCodeNotRecord");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticCodeNotSource => Get("Argument_DiagnosticCodeNotSource");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticFieldBlank => Get("Argument_DiagnosticFieldBlank");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticNull => Get("Argument_DiagnosticNull");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticRowOutOfRange => Get("Argument_DiagnosticRowOutOfRange");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticScopeUnknown => Get("Argument_DiagnosticScopeUnknown");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DiagnosticWorksheetBlank => Get("Argument_DiagnosticWorksheetBlank");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string ExpectedColumnName => Get("Argument_ExpectedColumnName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string FieldLengthMinimumExceedsMaximum => Get("Argument_FieldLengthMinimumExceedsMaximum");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string RangeBoundsInvalid => Get("Argument_RangeBoundsInvalid");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string RangeFieldName => Get("Argument_RangeFieldName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string RangeRuleNull => Get("Argument_RangeRuleNull");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string DuplicateFieldName => Get("Argument_DuplicateFieldName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string RecordValidationRuleNull => Get("Argument_RecordValidationRuleNull");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string FailedWithoutSourceError => Get("Argument_FailedWithoutSourceError");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PositionMappingDuplicate => Get("Argument_PositionMappingDuplicate");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PositionMappingNull => Get("Argument_PositionMappingNull");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string PositionMappingUndeclared => Get("Argument_PositionMappingUndeclared");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string SourceDiagnosticHasRow => Get("Argument_SourceDiagnosticHasRow");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
        public static string SourcePositionName => Get("Argument_SourcePositionName");
        /// <summary>Gets the invariant, resource-backed get message without including source data.</summary>
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
