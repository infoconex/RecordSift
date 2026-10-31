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
        /// <summary>Defines a source column with independently configured presence, value-required and inclusive raw string-length constraints.</summary>
        /// <param name="name">Exact-case source field name.</param>
        /// <param name="isRequired">Whether the column must appear in the source.</param>
        public ExpectedColumn(string name, bool isRequired)
            : this(name, isRequired, false)
        {
        }

        /// <summary>Defines a source column with independently configured presence, value-required and inclusive raw string-length constraints.</summary>
        /// <param name="name">Exact-case source field name.</param>
        /// <param name="isRequired">Whether the column must appear in the source.</param>
        /// <param name="isValueRequired">Whether a present record must contain a nonblank field value.</param>
        public ExpectedColumn(string name, bool isRequired, bool isValueRequired)
            : this(name, isRequired, isValueRequired, null, null)
        {
        }

        /// <summary>Defines a source column with independently configured presence, value-required and inclusive raw string-length constraints.</summary>
        /// <param name="name">Exact-case source field name.</param>
        /// <param name="isRequired">Whether the column must appear in the source.</param>
        /// <param name="isValueRequired">Whether a present record must contain a nonblank field value.</param>
        /// <param name="minimumLength">Optional inclusive minimum number of UTF-16 code units.</param>
        /// <param name="maximumLength">Optional inclusive maximum number of UTF-16 code units.</param>
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

        /// <summary>Gets the exact-case source column identifier.</summary>
        public string Name { get; private set; }
        /// <summary>Gets whether the source column must exist in the header or positional mapping.</summary>
        public bool IsRequired { get; private set; }
        /// <summary>Gets whether a present record value must contain non-whitespace characters.</summary>
        public bool IsValueRequired { get; private set; }
        /// <summary>Inclusive bound on the supplied string's UTF-16 code units; null disables.</summary>
        public int? MinimumLength { get; private set; }
        /// <summary>Inclusive bound on the supplied string's UTF-16 code units; null disables.</summary>
        public int? MaximumLength { get; private set; }
    }
}
