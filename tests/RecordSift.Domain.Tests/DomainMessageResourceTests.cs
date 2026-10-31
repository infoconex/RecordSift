using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
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
                { DiagnosticCodes.SourceDecodingFailed, "The source text could not be decoded with the configured encoding." },
                { DiagnosticCodes.SourceLimitExceeded, "The source exceeded the configured read limit." },
                { DiagnosticCodes.RequiredHeaderMissing, "A required source header is missing." },
                { DiagnosticCodes.RequiredColumnMissing, "A required source column is missing." },
                { DiagnosticCodes.AmbiguousColumn, "A declared source column is ambiguous." },
                { DiagnosticCodes.InvalidColumnPosition, "A declared source column position is outside the available field range." },
                { DiagnosticCodes.InvalidCsvDelimiter, "The configured CSV delimiter is invalid." },
                { DiagnosticCodes.MalformedSource, "The source structure is malformed." },
                { DiagnosticCodes.WorksheetNotFound, "The requested worksheet was not found in the workbook." },
                { DiagnosticCodes.RequiredFieldMissing, "A required field is missing." },
                { DiagnosticCodes.ConversionFailed, "A field could not be converted." },
                { DiagnosticCodes.ValidationFailed, "A field failed validation." },
                { DiagnosticCodes.FieldLengthOutOfRange, "The field length is outside the configured range." },
                { DiagnosticCodes.FieldRangeOutOfRange, "The field value is outside the configured range." },
                { DiagnosticCodes.DuplicateIdentifier, "A duplicate record identifier was detected." }
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
            var codeFields = typeof(DiagnosticCodes).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(codeFields, Has.Length.EqualTo(HistoricalDiagnosticMessages.Count));
            foreach (var field in codeFields)
            {
                Assert.That(field.IsLiteral, Is.False, field.Name);
                Assert.That(field.IsInitOnly, Is.True, field.Name);
                Assert.That(field.FieldType, Is.EqualTo(typeof(string)), field.Name);
            }

            var codes = codeFields.Select(field => (string)field.GetValue(null)).ToArray();

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
            Assert.That(Catalog().GetString("Argument_PositionMappingDuplicate", CultureInfo.InvariantCulture),
                Is.EqualTo("A declared source column cannot have multiple positions."));
        }

        [Test]
        public void PublicDomainMessageContractExposesNamedPropertiesWithoutRawKeyApi()
        {
            var publicMembers = System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            Assert.That(typeof(DomainMessageResources).GetMethod("Get", publicMembers), Is.Null);

            var namedMessages = typeof(DomainMessageResources).GetProperties(publicMembers);
            Assert.That(namedMessages, Has.Length.EqualTo(39));
            foreach (var property in namedMessages)
            {
                Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));
                var key = "Argument_" + property.Name;
                Assert.That(property.GetValue(null, null),
                    Is.EqualTo(Catalog().GetString(key, CultureInfo.InvariantCulture)),
                    "Named resource contract for " + key);
            }
        }


        [Test]
        public void PublicDomainLibraryContractsMustNotExposeCompileTimeConstants()
        {
            var literals = typeof(ImportDiagnostic).Assembly.GetExportedTypes()
                .SelectMany(type => type.GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.DeclaredOnly))
                .Where(field => field.IsLiteral && !field.DeclaringType.IsEnum)
                .Select(field => field.DeclaringType.FullName + "." + field.Name)
                .ToArray();

            Assert.That(literals, Is.Empty,
                "Public compile-time constants are embedded into consuming assemblies.");
        }

        [Test]
        public void NamedDomainMessagesUseInvariantResourcesRegardlessOfUICulture()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(DomainMessageResources.PositionMappingDuplicate,
                    Is.EqualTo("A declared source column cannot have multiple positions."));
                Assert.That(DomainMessageResources.ColumnDeclarationNull,
                    Is.EqualTo("Column declarations cannot contain null."));
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

    }
}
