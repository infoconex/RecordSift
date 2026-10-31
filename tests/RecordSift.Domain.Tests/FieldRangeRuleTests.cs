using System;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class FieldRangeRuleTests
    {
        private sealed class Invoice
        {
            public decimal Amount { get; set; }
            public DateTime DueDate { get; set; }
        }

        [Test]
        public void ExclusiveNumericBoundariesRejectBothEndpointsAndAcceptInterior()
        {
            var rule = FieldRangeRule<Invoice>.Numeric(
                "Amount", x => x.Amount, 0m, false, 10m, false);
            Assert.That(rule.Validate(new Invoice { Amount = 0m }, 1, 3).Code,
                Is.EqualTo(DiagnosticCodes.FieldRangeOutOfRange));
            Assert.That(rule.Validate(new Invoice { Amount = 10m }, 2, 4).Field,
                Is.EqualTo("Amount"));
            Assert.That(rule.Validate(new Invoice { Amount = 5m }, 3), Is.Null);
        }

        [Test]
        public void ExclusiveDateBoundariesEvaluateTypedDatesWithoutTimezoneInference()
        {
            var start = new DateTime(2024, 1, 1);
            var end = new DateTime(2024, 12, 31);
            var rule = FieldRangeRule<Invoice>.Date(
                "DueDate", x => x.DueDate, start, false, end, false);
            Assert.That(rule.Validate(new Invoice { DueDate = start }, 1).Code,
                Is.EqualTo(DiagnosticCodes.FieldRangeOutOfRange));
            Assert.That(rule.Validate(new Invoice { DueDate = new DateTime(2024, 6, 1) }, 2),
                Is.Null);
            Assert.That(rule.Validate(new Invoice { DueDate = end }, 3).Code,
                Is.EqualTo(DiagnosticCodes.FieldRangeOutOfRange));
        }

        [Test]
        public void RejectsContradictoryRangeDeclarationsAndMissingSelectors()
        {
            Assert.Throws<ArgumentException>(() =>
                FieldRangeRule<Invoice>.Numeric("Amount", x => x.Amount,
                    20m, true, 10m, true));
            Assert.Throws<ArgumentException>(() =>
                FieldRangeRule<Invoice>.Numeric("Amount", x => x.Amount,
                    10m, false, 10m, true));
            Assert.Throws<ArgumentException>(() =>
                FieldRangeRule<Invoice>.Date("DueDate", x => x.DueDate,
                    new DateTime(2024, 12, 31), true,
                    new DateTime(2024, 1, 1), true));
            Assert.Throws<ArgumentException>(() =>
                FieldRangeRule<Invoice>.Numeric(" ", x => x.Amount,
                    0m, true, 1m, true));
            Assert.Throws<ArgumentNullException>(() =>
                FieldRangeRule<Invoice>.Numeric("Amount", null,
                    0m, true, 1m, true));
        }

        [Test]
        public void AnUnboundedRangeDeclarationIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                FieldRangeRule<Invoice>.Numeric("Amount", x => x.Amount,
                    null, true, null, true));
            Assert.Throws<ArgumentException>(() =>
                FieldRangeRule<Invoice>.Date("DueDate", x => x.DueDate,
                    null, true, null, true));
        }
    }
}
