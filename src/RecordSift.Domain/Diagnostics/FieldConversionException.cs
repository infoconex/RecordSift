using System;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Safe failure preserving the declared source field, without raw value or vendor exception text.</summary>
    public sealed class FieldConversionException : FormatException
    {
        /// <summary>Creates a safe field-conversion exception that does not retain the rejected source value.</summary>
        /// <param name="field">Optional trusted source-field metadata; null indicates no field.</param>
        public FieldConversionException(string field)
            : base(DomainMessageResources.ConversionInvalidValue)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DiagnosticFieldBlank, nameof(field));
            Field = field;
        }

        /// <summary>Gets the declared source field associated with the conversion failure.</summary>
        public string Field { get; private set; }
    }
}
