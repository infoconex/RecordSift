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

        public static string Get(string resourceKey)
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
