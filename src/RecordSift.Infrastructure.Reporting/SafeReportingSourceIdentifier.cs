using System;

namespace RecordSift.Infrastructure.Reporting
{
    // Matches the original JSON adapter's safe-source policy for CSV and JSON.
    internal static class SafeReportingSourceIdentifier
    {
        internal static string Sanitize(string sourceId)
        {
            bool rooted = sourceId.StartsWith("/", StringComparison.Ordinal) ||
                sourceId[0] == (char)92 ||
                sourceId.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                (sourceId.Length >= 3 && char.IsLetter(sourceId[0]) &&
                 sourceId[1] == ':' && (sourceId[2] == '/' || sourceId[2] == (char)92));
            return rooted ? "source-redacted" : sourceId;
        }
    }
}
