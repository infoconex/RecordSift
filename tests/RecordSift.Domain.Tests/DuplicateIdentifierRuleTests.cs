using System;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class DuplicateIdentifierRuleTests
    {
        private sealed class Customer
        {
            public string Id { get; set; }
            public int NumericId { get; set; }
        }

        [Test]
        public void CaseInsensitivePolicyFlagsLaterDuplicateWithoutLeakingIdentifier()
        {
            var rule = DuplicateIdentifierRule<Customer>.ForKey(
                "Id", x => x.Id, StringComparer.OrdinalIgnoreCase);
            var validate = rule.CreateValidator();

            Assert.That(validate(new Customer { Id = "A-1" }, 1, 2), Is.Null);
            var diagnostic = validate(new Customer { Id = "a-1" }, 2, 3);
            Assert.That(diagnostic.Code, Is.EqualTo(DiagnosticCodes.DuplicateIdentifier));
            Assert.That(diagnostic.Row, Is.EqualTo(2));
            Assert.That(diagnostic.PhysicalLine, Is.EqualTo(3));
            Assert.That(diagnostic.Field, Is.EqualTo("Id"));
            Assert.That(diagnostic.Message, Does.Not.Contain("A-1"));
        }

        [Test]
        public void DefaultTypedEqualityAndNewImportSessionAreIndependent()
        {
            var rule = DuplicateIdentifierRule<Customer>.ForKey("NumericId", x => x.NumericId);
            var first = rule.CreateValidator();
            Assert.That(first(new Customer { NumericId = 42 }, 1, null), Is.Null);
            Assert.That(first(new Customer { NumericId = 43 }, 2, null), Is.Null);
            Assert.That(first(new Customer { NumericId = 42 }, 3, null).Code,
                Is.EqualTo(DiagnosticCodes.DuplicateIdentifier));

            var second = rule.CreateValidator();
            Assert.That(second(new Customer { NumericId = 42 }, 1, null), Is.Null);
        }

        [Test]
        public void NullIdentifierDoesNotBecomeAnImplicitRequiredValue()
        {
            var rule = DuplicateIdentifierRule<Customer>.ForKey("Id", x => x.Id);
            var validate = rule.CreateValidator();
            Assert.That(validate(new Customer { Id = null }, 1, null), Is.Null);
            Assert.That(validate(new Customer { Id = null }, 2, null), Is.Null);
        }

        [Test]
        public void InvalidIdentifierDeclarationIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                DuplicateIdentifierRule<Customer>.ForKey(" ", x => x.Id));
            Assert.Throws<ArgumentNullException>(() =>
                DuplicateIdentifierRule<Customer>.ForKey<string>("Id", null));
        }
    }
}
