using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using NUnit.Framework;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Tests
{
    [TestFixture]
    public sealed class DomainMessageResourceTests
    {
        private const string ResourceBaseName = "RecordSift.Resources.DomainMessages";

        private static ResourceManager Catalog()
        {
            return new ResourceManager(ResourceBaseName, typeof(ImportDiagnostic).Assembly);
        }

        private static readonly IDictionary<string, string> HistoricalDiagnosticMessages =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { DiagnosticCodes.SourceUnreadable, "The source could not be read." },
                { DiagnosticCodes.SourceLimitExceeded, "The source exceeded the configured read limit." },
                { DiagnosticCodes.RequiredHeaderMissing, "A required source header is missing." },
                { DiagnosticCodes.RequiredColumnMissing, "A required source column is missing." },
                { DiagnosticCodes.AmbiguousColumn, "A declared source column is ambiguous." },
                { DiagnosticCodes.InvalidColumnPosition, "A declared source column position is outside the available field range." },
                { DiagnosticCodes.InvalidCsvDelimiter, "The configured CSV delimiter is invalid." },
                { DiagnosticCodes.MalformedSource, "The source structure is malformed." },
                { DiagnosticCodes.RequiredFieldMissing, "A required field is missing." },
                { DiagnosticCodes.ConversionFailed, "A field could not be converted." },
                { DiagnosticCodes.ValidationFailed, "A field failed validation." }
            };

        [Test]
        public void DomainMessagesAreEmbeddedInCoreNotInVendorAdapter()
        {
            Assert.That(typeof(ImportDiagnostic).Assembly.GetManifestResourceNames(),
                Does.Contain(ResourceBaseName + ".resources"));
        }

        [Test]
        public void EveryRegisteredDiagnosticCodeMapsToUnchangedSafeResourceText()
        {
            var resourceManager = Catalog();
            var codes = typeof(DiagnosticCodes).GetFields(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .ToArray();

            Assert.That(codes, Is.EquivalentTo(HistoricalDiagnosticMessages.Keys));
            foreach (var code in codes)
            {
                var resource = resourceManager.GetString(code, CultureInfo.InvariantCulture);
                Assert.That(resource, Is.EqualTo(HistoricalDiagnosticMessages[code]),
                    "Resource contract for " + code);
                var diagnostic = DiagnosticCodes.IsRecordCode(code)
                    ? ImportDiagnostic.ForRecord(1, null, code)
                    : ImportDiagnostic.ForSource(null, code);
                Assert.That(diagnostic.Code, Is.EqualTo(code));
                Assert.That(diagnostic.Message, Is.EqualTo(resource));
            }
        }

        [Test]
        public void DiagnosticMessagesRemainDeterministicRegardlessOfAmbientUICulture()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                var diagnostic = ImportDiagnostic.ForSource(null, DiagnosticCodes.MalformedSource);
                Assert.That(diagnostic.Message,
                    Is.EqualTo("The source structure is malformed."));
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Test]
        public void ArgumentValidationMessagesHaveCentralResourceEntries()
        {
            Assert.That(Catalog().GetString("Argument_ColumnDeclarationNull", CultureInfo.InvariantCulture),
                Is.EqualTo("Column declarations cannot contain null."));
            Assert.That(Catalog().GetString("Argument_ReaderHeaderConflict", CultureInfo.InvariantCulture),
                Is.EqualTo("Headed sources use named header bindings, not positions."));
        }

        [Test]
        public void CsvDefaultDelimiterIsOwnedByCoreConfiguration()
        {
            Assert.That(CsvReadOptions.Default.Delimiter, Is.EqualTo(","));
            Assert.That(CsvReadOptions.Default.IsValid, Is.True);
        }
    }
}
