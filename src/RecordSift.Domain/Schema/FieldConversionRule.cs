using System;
using System.Globalization;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Format-neutral, exact-case converter registration for one declared source field.
    /// The caller supplies conversion policy; Application owns sanitized error translation.
    /// </summary>
    public sealed class FieldConversionRule
    {
        private readonly Func<string, CultureInfo, object> _convert;

        /// <summary>Registers a converter for the exact-case canonical field name.</summary>
        /// <param name="field">Declared canonical source field identifier; not an alias.</param>
        /// <param name="convert">Caller-provided conversion function receiving raw value and explicit read-only culture.</param>
        public FieldConversionRule(string field, Func<string, CultureInfo, object> convert)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DiagnosticFieldBlank, nameof(field));
            if (convert == null)
                throw new ArgumentNullException(nameof(convert));
            Field = field;
            _convert = convert;
        }

        /// <summary>Gets the canonical, exact-case field identifier for this converter.</summary>
        public string Field { get; private set; }

        /// <summary>
        /// Invokes the configured converter without swallowing programming exceptions.
        /// Use the Application mapper to turn input-specific converter failures into safe diagnostics.
        /// </summary>
        /// <param name="value">Raw source field contents; may be null.</param>
        /// <param name="culture">Explicit culture provided by the caller; null is invalid.</param>
        /// <returns>Boxed destination value produced by the converter.</returns>
        public object Convert(string value, CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));
            return _convert(value, culture);
        }
    }
}
