using System.Globalization;
using System.Resources;

namespace RecordSift
{
    /// <summary>
    /// Core-owned resource catalog: stable machine codes and invariant messages.
    /// The adapter never owns these descriptions or exposes vendor exception text.
    /// </summary>
    internal static class DomainMessageResources
    {
        private static readonly ResourceManager Catalog = new ResourceManager(
            "RecordSift.Resources.DomainMessages", typeof(DomainMessageResources).Assembly);

        internal static string Get(string resourceKey)
        {
            // Fixed invariant-culture report messages are part of release 0.1's
            // observable contract. Presentation localization can be added separately.
            string message = Catalog.GetString(resourceKey, CultureInfo.InvariantCulture);
            if (message == null)
                throw new MissingManifestResourceException();
            return message;
        }
    }
}
