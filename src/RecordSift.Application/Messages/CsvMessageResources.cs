using System.Globalization;
using System.Resources;

namespace RecordSift.Application.Messages
{
    /// <summary>
    /// Application-owned resource catalog for CSV port and table validation.
    /// The stable Domain diagnostic identifiers and safe messages stay in Domain.
    /// </summary>
    public static class CsvMessageResources
    {
        private static readonly ResourceManager s_catalog = new ResourceManager(
            "RecordSift.Resources.CsvMessages", typeof(CsvMessageResources).Assembly);

        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ImportCompleted => Get("Outcome_ImportCompleted");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ImportCompletedWithErrors => Get("Outcome_ImportCompletedWithErrors");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ImportNoDataRecords => Get("Outcome_ImportNoDataRecords");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ImportEmptySource => Get("Outcome_ImportEmptySource");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ImportMissingHeader => Get("Outcome_ImportMissingHeader");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ImportFailed => Get("Outcome_ImportFailed");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ReaderHeaderConflict => Get("Argument_ReaderHeaderConflict");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string HeaderlessMissingPositions => Get("Argument_HeaderlessMissingPositions");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string FailedTableNoError => Get("Argument_FailedTableNoError");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string FailedTableNonSourceError => Get("Argument_FailedTableNonSourceError");
        /// <summary>Gets an invariant resource-backed get message.</summary>
        public static string ParserErrorScope => Get("Argument_ParserErrorScope");

        internal static string Get(string resourceKey)
        {
            string message = s_catalog.GetString(resourceKey, CultureInfo.InvariantCulture);
            if (message == null)
                throw new MissingManifestResourceException();
            return message;
        }
    }
}
