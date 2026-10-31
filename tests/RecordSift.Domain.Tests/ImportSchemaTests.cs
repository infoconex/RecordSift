using System;
using System.Collections.Generic;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    public sealed class ImportSchemaTests
    {
        // FR-009 tests bind supplied source-header metadata; no file reader is implemented here.
        [Test]
        public void FR_009_SC01_RequiredColumnsBindToSourcePositionsRegardlessOfOrder()
        {
            var schema = new ImportSchema(new[]
            {
                new ExpectedColumn("CustomerId", true),
                new ExpectedColumn("Amount", true)
            });

            var result = schema.BindHeaders(new[] { "Extra", "Amount", "CustomerId" });

            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Bindings, Has.Count.EqualTo(2));
            Assert.That(result.Bindings[0].Name, Is.EqualTo("CustomerId"));
            Assert.That(result.Bindings[0].SourceIndex, Is.EqualTo(2));
            Assert.That(result.Bindings[1].Name, Is.EqualTo("Amount"));
            Assert.That(result.Bindings[1].SourceIndex, Is.EqualTo(1));
        }

        [Test]
        public void FR_009_SC02_MissingRequiredSourceColumnReturnsSourceDiagnostic()
        {
            var schema = new ImportSchema(new[]
            {
                new ExpectedColumn("CustomerId", true),
                new ExpectedColumn("Amount", true)
            });
            var result = schema.BindHeaders(new[] { "CustomerId" });

            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Bindings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredColumnMissing));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Amount"));
        }

        [Test]
        public void MissingOptionalColumnIsNotAnError()
        {
            var schema = new ImportSchema(new[]
            {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Notes", false)
            });
            var result = schema.BindHeaders(new[] { "Id" });
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void DuplicateDeclaredNameIsRejectedAtConfigurationTime()
        {
            Assert.Throws<ArgumentException>(() => new ImportSchema(new[]
            {
                new ExpectedColumn("Id", true), new ExpectedColumn("Id", false)
            }));
        }

        [Test]
        public void DuplicateRequiredSourceHeaderIsReportedAsAmbiguous()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            var result = schema.BindHeaders(new[] { "Id", "Other", "Id" });
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.AmbiguousColumn));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Bindings, Is.Empty);
        }

        [Test]
        public void DuplicateOptionalSourceHeaderIsAlsoAmbiguous()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Notes", false) });
            var result = schema.BindHeaders(new[] { "Notes", "Notes" });
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.AmbiguousColumn));
        }

        [Test]
        public void UnexpectedDuplicateSourceColumnsDoNotPreventKnownBindings()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            var result = schema.BindHeaders(new[] { "Extra", "Extra", "Id" });
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings[0].SourceIndex, Is.EqualTo(2));
        }

        [Test]
        public void BindingNamesAreCaseSensitiveAndCultureIndependent()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            var result = schema.BindHeaders(new[] { "id" });
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredColumnMissing));
        }

        [Test]
        public void AllMissingRequiredColumnsAreReportedInDeclarationOrder()
        {
            var schema = new ImportSchema(new[]
            {
                new ExpectedColumn("A", true), new ExpectedColumn("B", true)
            });
            var result = schema.BindHeaders(new string[0]);
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("A"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("B"));
        }

        [Test]
        public void EmptyHeaderListWithOptionalColumnsSucceedsWithoutBindings()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Notes", false) });
            var result = schema.BindHeaders(new string[0]);
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings, Is.Empty);
        }

        [Test]
        public void SchemaCopiesDeclarationsAndExposesReadOnlyBindings()
        {
            var declarations = new List<ExpectedColumn> { new ExpectedColumn("Id", true) };
            var schema = new ImportSchema(declarations);
            declarations.Clear();
            var result = schema.BindHeaders(new[] { "Id" });
            Assert.That(schema.Columns, Has.Count.EqualTo(1));
            Assert.That(result.Bindings, Has.Count.EqualTo(1));
            Assert.That(schema.Columns, Is.InstanceOf<IReadOnlyList<ExpectedColumn>>());
            Assert.That(((IList<SourceColumnBinding>)result.Bindings).IsReadOnly, Is.True);
        }

        [Test]
        public void NullAndEmptyConfigurationAndInputAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new ImportSchema(null));
            Assert.Throws<ArgumentException>(() => new ExpectedColumn(null, true));
            Assert.Throws<ArgumentException>(() => new ExpectedColumn(" ", true));
            Assert.Throws<ArgumentException>(() => new ImportSchema(new ExpectedColumn[] { null }));
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            Assert.Throws<ArgumentNullException>(() => schema.BindHeaders(null));
        }

        [Test]
        public void BlankOrNullSourceHeadersDoNotAccidentallyBindRequiredColumns()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            var result = schema.BindHeaders(new string[] { null, " ", "Id" });
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings[0].SourceIndex, Is.EqualTo(2));
        }

        [Test]
        public void FR_017_SC01_RequiredValuePolicyReportsBlankFieldsWithSourceProvenance()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Name", true, true),
                new ExpectedColumn("Notes", false)
            });
            var values = new Dictionary<string, string>(StringComparer.Ordinal) {
                { "Id", " " }, { "Name", "" }, { "Notes", "" }
            };

            var errors = schema.ValidateRequiredValues(values, 2, 5);

            Assert.That(errors, Has.Count.EqualTo(2));
            Assert.That(errors[0].Code, Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
            Assert.That(errors[0].Field, Is.EqualTo("Id"));
            Assert.That(errors[0].Row, Is.EqualTo(2));
            Assert.That(errors[0].PhysicalLine, Is.EqualTo(5));
            Assert.That(errors[1].Field, Is.EqualTo("Name"));
        }

        [Test]
        public void FR_017_SC02_NonblankRequiredValuesAndBlankOptionalValuesAreAccepted()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true),
                new ExpectedColumn("Notes", false)
            });
            var values = new Dictionary<string, string> {
                { "Id", "  42  " }, { "Notes", "" }
            };

            Assert.That(schema.ValidateRequiredValues(values, 1), Is.Empty);
        }

        [Test]
        public void RequiredValueRuleRejectsNullAndUsesOrdinalSourceIdentifiers()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true, true)
            });
            var missing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                { "id", "42" }
            };
            var noValue = new Dictionary<string, string> {
                { "Id", null }
            };

            Assert.That(schema.ValidateRequiredValues(missing, 1)[0].Field,
                Is.EqualTo("Id"));
            Assert.That(schema.ValidateRequiredValues(noValue, 1)[0].Code,
                Is.EqualTo(DiagnosticCodes.RequiredFieldMissing));
        }

        [Test]
        public void ExistingRequiredColumnPresenceDoesNotImplicitlyRequireNonblankValues()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Id", true)
            });
            var values = new Dictionary<string, string> {
                { "Id", "" }
            };

            Assert.That(schema.ValidateRequiredValues(values, 1), Is.Empty);
            Assert.That(schema.Columns[0].IsRequired, Is.True);
            Assert.That(schema.Columns[0].IsValueRequired, Is.False);
        }
    }
}
