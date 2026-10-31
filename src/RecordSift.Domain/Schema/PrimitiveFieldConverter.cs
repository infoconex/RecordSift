using System;
using System.Globalization;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Supported .NET primitive field conversions, independent of source format and ambient culture.</summary>
    public sealed class PrimitiveFieldConverter
    {
        private readonly CultureInfo _culture;

        public PrimitiveFieldConverter(CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));
            _culture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        }

        public bool IsSupported(Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));
            var target = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            return target == typeof(string) || target == typeof(int) ||
                target == typeof(decimal) || target == typeof(DateTime) ||
                target == typeof(bool);
        }

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
                decimal.TryParse(value, NumberStyles.Number, _culture, out money))
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
    }
}
