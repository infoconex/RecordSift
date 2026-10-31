using System;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Safe failure preserving the declared source field, without raw value or vendor exception text.</summary>
    public sealed class FieldConversionException : FormatException
    {
        public FieldConversionException(string field)
            : base(DomainMessageResources.ConversionInvalidValue)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DiagnosticFieldBlank, nameof(field));
            Field = field;
        }

        public string Field { get; private set; }
    }
}
