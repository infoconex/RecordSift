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
    }
}
