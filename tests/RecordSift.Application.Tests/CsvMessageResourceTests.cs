using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using NUnit.Framework;
using RecordSift.Application.Messages;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    public sealed class CsvMessageResourceTests
    {
        private const string ResourceBaseName = "RecordSift.Resources.CsvMessages";

        private static ResourceManager Catalog()
        {
            return new ResourceManager(ResourceBaseName, typeof(CsvMessageResources).Assembly);
        }

        [Test]
        public void ApplicationOwnsAllCsvValidationMessagesAndDomainDoesNot()
        {
            Assert.That(typeof(CsvMessageResources).Assembly.GetManifestResourceNames(),
                Does.Contain(ResourceBaseName + ".resources"));
            Assert.That(typeof(ImportDiagnostic).Assembly.GetManifestResourceNames(),
                Does.Not.Contain(ResourceBaseName + ".resources"));
            var domainCatalog = new ResourceManager("RecordSift.Resources.DomainMessages",
                typeof(ImportDiagnostic).Assembly);
            Assert.That(domainCatalog.GetString("Argument_ReaderHeaderConflict",
                CultureInfo.InvariantCulture), Is.Null);
            Assert.That(Catalog().GetString("Argument_ReaderHeaderConflict",
                CultureInfo.InvariantCulture),
                Is.EqualTo("Headed sources use named header bindings, not positions."));
        }

        [Test]
        public void CsvMessageResourcesHaveNamedTypedAccessorsWithoutPublicRawLookup()
        {
            var flags = BindingFlags.Public | BindingFlags.Static;
            Assert.That(typeof(CsvMessageResources).GetMethod("Get", flags), Is.Null);
            var properties = typeof(CsvMessageResources).GetProperties(flags);
            Assert.That(properties, Has.Length.EqualTo(5));
            foreach (var property in properties)
            {
                Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));
                var key = "Argument_" + property.Name;
                Assert.That(property.GetValue(null, null),
                    Is.EqualTo(Catalog().GetString(key, CultureInfo.InvariantCulture)),
                    "CSV resource contract for " + key);
            }
        }

        [Test]
        public void CsvArgumentValidationMessagesAreInvariantAcrossUICultures()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(CsvMessageResources.ReaderHeaderConflict,
                    Is.EqualTo("Headed sources use named header bindings, not positions."));
                Assert.That(CsvMessageResources.FailedTableNoError,
                    Is.EqualTo("Failed CSV table must identify a source error."));
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}
