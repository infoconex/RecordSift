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
        public static string FailedTableNoError => Get("Argument_FailedTableNoError");
        public static string FailedTableNonSourceError => Get("Argument_FailedTableNonSourceError");
        public static string FailedWithoutSourceError => Get("Argument_FailedWithoutSourceError");
        public static string HeaderlessMissingPositions => Get("Argument_HeaderlessMissingPositions");
        public static string ParserErrorScope => Get("Argument_ParserErrorScope");
        public static string PositionMappingDuplicate => Get("Argument_PositionMappingDuplicate");
        public static string PositionMappingNull => Get("Argument_PositionMappingNull");
        public static string PositionMappingUndeclared => Get("Argument_PositionMappingUndeclared");
        public static string ReaderHeaderConflict => Get("Argument_ReaderHeaderConflict");
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
