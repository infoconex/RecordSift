using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Declares source-column presence, per-record required values, and optional
    /// raw string-length bounds. These are independent schema contracts.
    /// </summary>
    public sealed class ExpectedColumn
    {
        public ExpectedColumn(string name, bool isRequired)
            : this(name, isRequired, false)
        {
        }

        public ExpectedColumn(string name, bool isRequired, bool isValueRequired)
            : this(name, isRequired, isValueRequired, null, null)
        {
        }

        public ExpectedColumn(string name, bool isRequired, bool isValueRequired,
            int? minimumLength, int? maximumLength)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(DomainMessageResources.ExpectedColumnName, nameof(name));
            if (minimumLength.HasValue && minimumLength.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            if (maximumLength.HasValue && maximumLength.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumLength));
            if (minimumLength.HasValue && maximumLength.HasValue &&
                minimumLength.Value > maximumLength.Value)
                throw new ArgumentException(
                    DomainMessageResources.FieldLengthMinimumExceedsMaximum, nameof(minimumLength));

            Name = name;
            IsRequired = isRequired;
            IsValueRequired = isValueRequired;
            MinimumLength = minimumLength;
            MaximumLength = maximumLength;
        }

        public string Name { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsValueRequired { get; private set; }
        /// <summary>Inclusive bound on the supplied string's UTF-16 code units; null disables.</summary>
        public int? MinimumLength { get; private set; }
        /// <summary>Inclusive bound on the supplied string's UTF-16 code units; null disables.</summary>
        public int? MaximumLength { get; private set; }
    }
}
