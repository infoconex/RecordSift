using System.Globalization;
using System.Resources;

namespace RecordSift.Infrastructure.Reporting
{
    /// <summary>Invariant, safe reporting messages independent of caller-provided message text.</summary>
    internal static class ReportingMessageResources
    {
        private static readonly ResourceManager s_catalog = new ResourceManager(
            "RecordSift.Resources.ReportingMessages", typeof(ReportingMessageResources).Assembly);

        internal static string ImportCompleted => Get("Outcome_ImportCompleted");
        internal static string ImportCompletedWithRejections => Get("Outcome_ImportCompletedWithRejections");
        internal static string ImportEmpty => Get("Outcome_ImportEmpty");
        internal static string ImportFailed => Get("Outcome_ImportFailed");
        internal static string ImportStopped => Get("Outcome_ImportStopped");
        internal static string ReadableStoppedSummary => Get("Readable_StoppedSummary");
        internal static string ReadableStoppedSingleErrorSummary => Get("Readable_StoppedSingleErrorSummary");

        internal static string ReadableCompletedSummary => Get("Readable_CompletedSummary");
        internal static string ReadableFailedSummary => Get("Readable_FailedSummary");
        internal static string ReadableCompletedSingleErrorSummary => Get("Readable_CompletedSingleErrorSummary");
        internal static string ReadableFailedSingleErrorSummary => Get("Readable_FailedSingleErrorSummary");
        internal static string ReadableNoRecords => Get("Readable_NoRecords");
        internal static string ReadableRecordIssue => Get("Readable_RecordIssue");
        internal static string ReadableRecordFieldIssue => Get("Readable_RecordFieldIssue");
        internal static string ReadableSourceIssue => Get("Readable_SourceIssue");
        internal static string ReadableSourceFieldIssue => Get("Readable_SourceFieldIssue");
        internal static string ReadablePhysicalLine => Get("Readable_PhysicalLine");
        internal static string ReadablePhysicalRow => Get("Readable_PhysicalRow");
        internal static string ReadableWorksheet => Get("Readable_Worksheet");

        private static string Get(string resourceKey)
        {
            var message = s_catalog.GetString(resourceKey, CultureInfo.InvariantCulture);
            if (message == null)
                throw new MissingManifestResourceException();
            return message;
        }
    }
}
