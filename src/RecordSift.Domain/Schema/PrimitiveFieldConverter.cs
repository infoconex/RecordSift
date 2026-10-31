using System;
using System.Globalization;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Supported .NET primitive field conversions, independent of source format and ambient culture.</summary>
    public sealed class PrimitiveFieldConverter
    {
        // Decimal always has at most 28 fractional digits. The fixed-point format
        // avoids scientific notation when comparing exact numeric values.
        private const string ExactDecimalFormat = "0.############################";

        private readonly CultureInfo _culture;

        /// <summary>Creates a converter with a defensive, read-only snapshot of the supplied culture.</summary>
        /// <param name="culture">Explicit culture used for parsing, cloned to avoid later mutation.</param>
        public PrimitiveFieldConverter(CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            _culture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        }

        /// <summary>Reports whether a CLR destination type is supported, including nullable forms.</summary>
        /// <param name="destinationType">Requested CLR type; null is invalid.</param>
        public bool IsSupported(Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));
            var target = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            return target == typeof(string) || target == typeof(int) ||
                target == typeof(decimal) || target == typeof(DateTime) ||
                target == typeof(bool);
        }

        /// <summary>Converts a source string to string, Int32, Decimal, DateTime or Boolean (including nullable value types) using the captured culture; rejects rounded decimal conversions.</summary>
        /// <param name="value">Raw source string; null is preserved for a string destination.</param>
        /// <param name="destinationType">Requested CLR type; null is invalid.</param>
        /// <returns>The converted boxed value; null for a blank nullable value or a null source string mapped as string.</returns>
        /// <exception cref="ArgumentNullException">The destination type is null.</exception>
        /// <exception cref="ArgumentException">The destination CLR type is unsupported.</exception>
        /// <exception cref="FormatException">The source value is invalid or would require a lossy decimal conversion.</exception>
        public object Convert(string value, Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));
            if (!IsSupported(destinationType))
                throw new ArgumentException(DomainMessageResources.ConversionUnsupportedType,
                    nameof(destinationType));

            var target = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            if (target == typeof(string))
                return value;
            if (Nullable.GetUnderlyingType(destinationType) != null &&
                string.IsNullOrWhiteSpace(value))
                return null;

            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException(DomainMessageResources.ConversionInvalidValue);

            int integer;
            if (target == typeof(int) &&
                int.TryParse(value, NumberStyles.Integer, _culture, out integer))
                return integer;

            decimal money;
            if (target == typeof(decimal) &&
                decimal.TryParse(value, NumberStyles.Number, _culture, out money) &&
                HasExactDecimalValue(value, money, _culture.NumberFormat))
                return money;

            DateTime date;
            if (target == typeof(DateTime) &&
                DateTime.TryParse(value, _culture, DateTimeStyles.None, out date))
                return date;

            bool boolean;
            if (target == typeof(bool) && bool.TryParse(value, out boolean))
                return boolean;

            throw new FormatException(DomainMessageResources.ConversionInvalidValue);
        }

        private static bool HasExactDecimalValue(string input, decimal converted,
            NumberFormatInfo format)
        {
            // decimal.TryParse may round extra significant fractional digits.
            // Compare the original mathematical value with the full fixed-point
            // representation of the parsed decimal before accepting it.
            var original = NormalizeDecimalText(input, format);
            var parsed = NormalizeDecimalText(
                converted.ToString(ExactDecimalFormat, format), format);
            return string.Equals(original, parsed, StringComparison.Ordinal);
        }

        private static string NormalizeDecimalText(string value, NumberFormatInfo format)
        {
            var text = value.Trim();
            if (format.NegativeSign.Length > 0 &&
                text.StartsWith(format.NegativeSign, StringComparison.Ordinal))
                text = text.Substring(format.NegativeSign.Length);
            else if (format.PositiveSign.Length > 0 &&
                text.StartsWith(format.PositiveSign, StringComparison.Ordinal))
                text = text.Substring(format.PositiveSign.Length);

            text = text.Trim();
            if (format.NegativeSign.Length > 0 &&
                text.EndsWith(format.NegativeSign, StringComparison.Ordinal))
                text = text.Substring(0, text.Length - format.NegativeSign.Length);
            else if (format.PositiveSign.Length > 0 &&
                text.EndsWith(format.PositiveSign, StringComparison.Ordinal))
                text = text.Substring(0, text.Length - format.PositiveSign.Length);

            text = text.Trim();
            if (format.NumberGroupSeparator.Length > 0 &&
                !string.Equals(format.NumberGroupSeparator, format.NumberDecimalSeparator,
                    StringComparison.Ordinal))
                text = text.Replace(format.NumberGroupSeparator, string.Empty);

            var separator = format.NumberDecimalSeparator;
            var index = text.IndexOf(separator, StringComparison.Ordinal);
            var whole = index < 0 ? text : text.Substring(0, index);
            var fraction = index < 0 ? string.Empty : text.Substring(index + separator.Length);

            whole = whole.TrimStart('0');
            if (whole.Length == 0)
                whole = "0";
            fraction = fraction.TrimEnd('0');
            return fraction.Length == 0 ? whole : whole + "." + fraction;
        }
    }
}
