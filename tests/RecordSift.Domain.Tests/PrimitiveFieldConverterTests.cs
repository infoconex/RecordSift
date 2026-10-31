using System;
using System.Globalization;
using NUnit.Framework;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class PrimitiveFieldConverterTests
    {
        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-011")]
        [Property("Scenario", "FR-011-SC01")]
        public void FR_011_SC01_ConvertsStringIntegerDecimalDateBooleanAndNullableValues()
        {
            var converter = new PrimitiveFieldConverter(CultureInfo.GetCultureInfo("en-US"));
            Assert.That(converter.Convert("Example", typeof(string)), Is.EqualTo("Example"));
            Assert.That(converter.Convert("123", typeof(int)), Is.EqualTo(123));
            Assert.That(converter.Convert("12.50", typeof(decimal)), Is.EqualTo(12.50m));
            Assert.That(converter.Convert("12/31/2025", typeof(DateTime)),
                Is.EqualTo(new DateTime(2025, 12, 31)));
            Assert.That(converter.Convert("true", typeof(bool)), Is.EqualTo(true));
            Assert.That(converter.Convert("", typeof(int?)), Is.Null);
            Assert.That(converter.Convert("7", typeof(int?)), Is.EqualTo(7));
            Assert.That(converter.Convert(" ", typeof(DateTime?)), Is.Null);
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-011")]
        [Property("Scenario", "FR-011-SC02")]
        public void FR_011_SC02_ExplicitCultureDeterminesDecimalAndDateParsing()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var french = new PrimitiveFieldConverter(CultureInfo.GetCultureInfo("fr-FR"));
                Assert.That(french.Convert("12,50", typeof(decimal)), Is.EqualTo(12.5m));
                Assert.That(french.Convert("31/12/2025", typeof(DateTime)),
                    Is.EqualTo(new DateTime(2025, 12, 31)));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                var english = new PrimitiveFieldConverter(CultureInfo.GetCultureInfo("en-US"));
                Assert.That(english.Convert("12.50", typeof(decimal)), Is.EqualTo(12.5m));
                Assert.That(english.Convert("12/31/2025", typeof(DateTime)),
                    Is.EqualTo(new DateTime(2025, 12, 31)));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void InvalidValuesFailSafelyWithoutEchoingRawInput()
        {
            var converter = new PrimitiveFieldConverter(CultureInfo.InvariantCulture);
            var exception = Assert.Throws<FormatException>(() =>
                converter.Convert("secret-value", typeof(int)));
            Assert.That(exception.Message, Does.Not.Contain("secret-value"));
            Assert.Throws<FormatException>(() => converter.Convert("", typeof(bool)));
            Assert.Throws<FormatException>(() => converter.Convert("2147483648", typeof(int)));
        }

        [Test]
        public void MissingNonnullableValueAndUnsupportedTargetsDoNotCoerceDefaults()
        {
            var converter = new PrimitiveFieldConverter(CultureInfo.InvariantCulture);
            Assert.Throws<FormatException>(() => converter.Convert(null, typeof(int)));
            Assert.Throws<ArgumentException>(() => converter.Convert("x", typeof(Guid)));
            Assert.Throws<ArgumentNullException>(() => new PrimitiveFieldConverter(null));
        }
    }
}
