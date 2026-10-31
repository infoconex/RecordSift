using System;
using NUnit.Framework;
using RecordSift.Application.Ports.Xlsx;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class XlsxReadLimitsTests
    {
        [Test]
        [Property("Requirement", "FR-002")]
        public void ReadLimitsRequirePositiveBudgetsAndExposeConfiguredValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxReadLimits(0, 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxReadLimits(1, 0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxReadLimits(1, 2, 0));
            var limits = new XlsxReadLimits(10, 5, 20);
            Assert.That(limits.MaxRows, Is.EqualTo(10));
            Assert.That(limits.MaxColumns, Is.EqualTo(5));
            Assert.That(limits.MaxCellCharacters, Is.EqualTo(20));
        }
    }
}
