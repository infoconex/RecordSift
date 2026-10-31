using System;
using System.Collections.Generic;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class ImportSchemaTests
    {
        // FR-009 tests bind supplied source-header metadata; no file reader is implemented here.
        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-009")]
        [Property("Scenario", "FR-009-SC01")]
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
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-009")]
        [Property("Scenario", "FR-009-SC02")]
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
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-017")]
        [Property("Scenario", "FR-017-SC01")]
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
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-017")]
        [Property("Scenario", "FR-017-SC02")]
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

        [Test]
        public void LengthConstraintsRejectInvalidDeclarationBounds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ExpectedColumn("Name", true, false, -1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ExpectedColumn("Name", true, false, 0, -1));
            Assert.Throws<ArgumentException>(() =>
                new ExpectedColumn("Name", true, false, 5, 4));
        }

        [Test]
        public void LengthPolicyPreservesOrdinalFieldNamesAndIgnoresAbsentOptionalColumns()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Note", false, false, 2, 4)
            });
            var wrongCase = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase) { { "note", "oversized" } };
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);

            Assert.That(schema.ValidateFieldLengths(wrongCase, 1), Is.Empty);
            Assert.That(schema.ValidateFieldLengths(empty, 1), Is.Empty);
            Assert.That(schema.ValidateFieldLengths(
                new Dictionary<string, string> { { "Note", "A" } }, 2, 5)[0].Code,
                Is.EqualTo(DiagnosticCodes.FieldLengthOutOfRange));
        }
        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-014")]
        [Property("Scenario", "FR-014-SC01")]
        public void FR_014_SC01_DeclaredAliasBindsToCanonicalFieldWithOriginalSourceIndex()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("CustomerId", true, new[] { "Client Number", "Customer Number" }),
                new ExpectedColumn("Name", true)
            });

            var bound = schema.BindHeaders(new[] { "Name", "Client Number" });

            Assert.That(bound.IsSuccessful, Is.True);
            Assert.That(bound.Bindings, Has.Count.EqualTo(2));
            Assert.That(bound.Bindings[0].Name, Is.EqualTo("CustomerId"));
            Assert.That(bound.Bindings[0].SourceIndex, Is.EqualTo(1));
            Assert.That(bound.Bindings[1].SourceIndex, Is.Zero);
            Assert.That(bound.Diagnostics, Is.Empty);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-014")]
        [Property("Scenario", "FR-014-SC02")]
        public void FR_014_SC02_AliasAndCanonicalOrTwoAliasesRejectAtomically()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("CustomerId", true, new[] { "Client Number", "Customer Number" }),
                new ExpectedColumn("Name", true)
            });

            foreach (var headers in new[] {
                new[] { "CustomerId", "Client Number", "Name" },
                new[] { "Client Number", "Customer Number", "Name" },
                new[] { "Client Number", "Client Number", "Name" }
            })
            {
                var bound = schema.BindHeaders(headers);
                Assert.That(bound.IsSuccessful, Is.False);
                Assert.That(bound.Bindings, Is.Empty);
                Assert.That(bound.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(bound.Diagnostics[0].Code,
                    Is.EqualTo(DiagnosticCodes.AmbiguousColumn));
                Assert.That(bound.Diagnostics[0].Field, Is.EqualTo("CustomerId"));
                Assert.That(bound.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            }
        }

        [Test]
        public void AliasesAreOrdinalDistinctImmutableAndCannotCollideWithAnotherDeclaration()
        {
            var aliases = new List<string> { "Client Number" };
            var column = new ExpectedColumn("CustomerId", true, aliases);
            aliases.Add("Extra");
            Assert.That(column.Aliases, Is.EquivalentTo(new[] { "Client Number" }));
            Assert.That(((IList<string>)column.Aliases).IsReadOnly, Is.True);

            var schema = new ImportSchema(new[] { column });
            Assert.That(schema.BindHeaders(new[] { "client number" }).IsSuccessful, Is.False);
            Assert.That(schema.BindHeaders(new[] { "CustomerId" }).IsSuccessful, Is.True);
            Assert.Throws<ArgumentException>(() =>
                new ExpectedColumn("CustomerId", true, new[] { "Client Number", "Client Number" }));
            Assert.Throws<ArgumentException>(() =>
                new ExpectedColumn("CustomerId", true, new[] { "CustomerId" }));
            Assert.Throws<ArgumentException>(() =>
                new ExpectedColumn("CustomerId", true, new string[] { null }));
            Assert.Throws<ArgumentException>(() =>
                new ImportSchema(new[] {
                    column, new ExpectedColumn("Client Number", true)
                }));
            Assert.Throws<ArgumentException>(() =>
                new ImportSchema(new[] {
                    column, new ExpectedColumn("Name", true, new[] { "Client Number" })
                }));
        }

        [Test]
        public void ConfiguredOptionalDefaultIsRetainedWithoutRewritingExistingColumnPolicies()
        {
            var declared = new ExpectedColumn("Note", false, false, "Not supplied");
            Assert.That(declared.DefaultValue, Is.EqualTo("Not supplied"));
            Assert.That(declared.IsRequired, Is.False);
            Assert.That(declared.IsValueRequired, Is.False);
            Assert.That(new ExpectedColumn("Note", false).DefaultValue, Is.Null);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-023")]
        [Property("Scenario", "FR-023-SC01")]
        public void FR_023_SC01_NonmatchingPatternReportsFieldAndPhysicalLine()
        {
            var schema = new ImportSchema(
                new[] { new ExpectedColumn("Code", true) },
                new[] { new FieldPatternRule("Code", "^[A-Z]{2}[0-9]{2}$") });
            var values = new Dictionary<string, string> { { "Code", "private-invalid-code" } };

            var errors = schema.ValidatePatterns(values, 3, 7);

            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(errors[0].Field, Is.EqualTo("Code"));
            Assert.That(errors[0].Row, Is.EqualTo(3));
            Assert.That(errors[0].PhysicalLine, Is.EqualTo(7));
            Assert.That(errors[0].Message, Does.Not.Contain("private-invalid-code"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-023")]
        [Property("Scenario", "FR-023-SC02")]
        public void FR_023_SC02_MatchingPatternAndAbsentOptionalFieldHaveNoDiagnostic()
        {
            var schema = new ImportSchema(
                new[] { new ExpectedColumn("Code", true), new ExpectedColumn("Note", false) },
                new[] { new FieldPatternRule("Code", "^[A-Z]{2}[0-9]{2}$"),
                    new FieldPatternRule("Note", "^n[0-9]+$") });
            Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                { "Code", "AB12" }
            }, 1), Is.Empty);
        }

        [Test]
        public void RegexRuleConfigurationIsBoundedValidAndOrdinal()
        {
            Assert.Throws<ArgumentException>(() =>
                new FieldPatternRule("Code", "(unclosed"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FieldPatternRule("Code", "^A$", TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FieldPatternRule("Code", "^A$", TimeSpan.FromDays(1)));
            var rule = new FieldPatternRule("Code", "^(a+)+$", TimeSpan.FromMilliseconds(1));
            var schema = new ImportSchema(new[] { new ExpectedColumn("Code", true) }, new[] { rule });
            Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                { "Code", new string('a', 2000) + "!" }
            }, 1), Has.Count.EqualTo(1));
            Assert.Throws<ArgumentException>(() => new ImportSchema(
                new[] { new ExpectedColumn("Code", true) },
                new[] { new FieldPatternRule("Unknown", "^X$") }));
            Assert.Throws<ArgumentException>(() => new ImportSchema(
                new[] { new ExpectedColumn("Code", true) },
                new[] { new FieldPatternRule("Code", "^A$"), new FieldPatternRule("Code", "^B$") }));
        }


        [Test]
        [Property("Requirement", "FR-023")]
        public void PatternValidationDistinguishesMissingOptionalFromPresentEmptyAndSnapshotsRules()
        {
            var rules = new List<FieldPatternRule> {
                new FieldPatternRule("Optional", "^$")
            };
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("Optional", false)
            }, rules);
            rules.Add(new FieldPatternRule("Optional", "^must-not-be-added$"));

            Assert.That(schema.ValidatePatterns(new Dictionary<string, string>(), 1), Is.Empty);
            Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                { "Optional", null }
            }, 2), Is.Empty);
            Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                { "Optional", "" }
            }, 3), Is.Empty);
            Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                { "Optional", "private-nonempty" }
            }, 4), Has.Count.EqualTo(1));
        }

        [Test]
        [Property("Requirement", "FR-023")]
        public void PatternValidationRemainsCaseSensitiveAndCultureInvariant()
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                var schema = new ImportSchema(new[] { new ExpectedColumn("Code", true) },
                    new[] { new FieldPatternRule("Code", "^I$") });
                System.Globalization.CultureInfo.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

                Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                    { "Code", "I" }
                }, 1), Is.Empty);
                Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                    { "Code", "ı" }
                }, 2), Has.Count.EqualTo(1));
                Assert.That(schema.ValidatePatterns(new Dictionary<string, string> {
                    { "Code", "i" }
                }, 3), Has.Count.EqualTo(1));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }
    }
}
