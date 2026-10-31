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

        private static string Get(string resourceKey)
        {
            var message = s_catalog.GetString(resourceKey, CultureInfo.InvariantCulture);
            if (message == null)
                throw new MissingManifestResourceException();
            return message;
        }
    }
}
