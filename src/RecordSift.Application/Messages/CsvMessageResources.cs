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

        public static string ReaderHeaderConflict => Get("Argument_ReaderHeaderConflict");
        public static string HeaderlessMissingPositions => Get("Argument_HeaderlessMissingPositions");
        public static string FailedTableNoError => Get("Argument_FailedTableNoError");
        public static string FailedTableNonSourceError => Get("Argument_FailedTableNonSourceError");
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
