using System;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class RecordValidationRuleTests
    {
        private sealed class Customer
        {
            public string Name { get; set; }
        }

        [Test]
        public void FieldRuleReturnsSanitizedDiagnosticWithProvenance()
        {
            var rule = new RecordValidationRule<Customer>("Name", x => x.Name.Length >= 3);
            var result = rule.Validate(new Customer { Name = "secret" .Substring(0, 2) }, 2, 5);

            Assert.That(result.Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(result.Field, Is.EqualTo("Name"));
            Assert.That(result.Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(result.Row, Is.EqualTo(2));
            Assert.That(result.PhysicalLine, Is.EqualTo(5));
            Assert.That(result.Message, Does.Not.Contain("se"));
        }

        [Test]
        public void RecordRuleUsesNullFieldAndPassingRuleReturnsNull()
        {
            var rule = new RecordValidationRule<Customer>(null, x => x.Name != "deny");
            Assert.That(rule.Validate(new Customer { Name = "allow" }, 1, 2), Is.Null);
            Assert.That(rule.Validate(new Customer { Name = "deny" }, 2, 3).Field, Is.Null);
        }

        [Test]
        public void RuleRejectsInvalidConfigurationAndRowLocations()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RecordValidationRule<Customer>("Name", null));
            Assert.Throws<ArgumentException>(() =>
                new RecordValidationRule<Customer>(" ", x => true));
            var rule = new RecordValidationRule<Customer>(null, x => true);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                rule.Validate(new Customer(), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                rule.Validate(new Customer(), 1, 0));
        }

        private sealed class NumericRange
        {
            public int Start { get; set; }
            public int End { get; set; }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-024")]
        [Property("Scenario", "FR-024-SC01")]
        public void FR_024_SC01_ConvertedNumericRelationshipReturnsSanitizedRecordDiagnostic()
        {
            var rule = new RecordValidationRule<NumericRange>(null,
                range => range.Start <= range.End);
            var invalid = rule.Validate(new NumericRange { Start = 5, End = 2 }, 3, 8);

            Assert.That(invalid, Is.Not.Null);
            Assert.That(invalid.Code, Is.EqualTo(DiagnosticCodes.ValidationFailed));
            Assert.That(invalid.Field, Is.Null);
            Assert.That(invalid.Scope, Is.EqualTo(DiagnosticScope.Record));
            Assert.That(invalid.Row, Is.EqualTo(3));
            Assert.That(invalid.PhysicalLine, Is.EqualTo(8));
            Assert.That(invalid.Message, Does.Not.Contain("5"));
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-024")]
        [Property("Scenario", "FR-024-SC02")]
        public void FR_024_SC02_ConsistentAndEqualNumericEndpointsPassTheSameRule()
        {
            var rule = new RecordValidationRule<NumericRange>(null,
                range => range.Start <= range.End);
            Assert.That(rule.Validate(new NumericRange { Start = 1, End = 3 }, 1), Is.Null);
            Assert.That(rule.Validate(new NumericRange { Start = 7, End = 7 }, 2), Is.Null);
        }
    }
}
