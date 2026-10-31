using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Applies a declared numeric or date interval to an already-converted CLR record.
    /// The caller supplies an explicit typed selector; no parser, reflection or
    /// source-format dependency is part of this Domain contract.
    /// </summary>
    public sealed class FieldRangeRule<T>
    {
        private readonly Func<T, bool> _outsideRange;

        private FieldRangeRule(string field, Func<T, bool> outsideRange)
        {
            Field = field;
            _outsideRange = outsideRange;
        }

        public string Field { get; private set; }

        public static FieldRangeRule<T> Numeric(string field, Func<T, decimal> selector,
            decimal? minimum, bool minimumInclusive, decimal? maximum, bool maximumInclusive)
        {
            ValidateFieldAndSelector(field, selector);
            if (!minimum.HasValue && !maximum.HasValue)
                throw new ArgumentException(DomainMessageResources.RangeBoundsInvalid,
                    nameof(minimum));
            if (minimum.HasValue && maximum.HasValue &&
                (minimum.Value > maximum.Value ||
                 (minimum.Value == maximum.Value &&
                  (!minimumInclusive || !maximumInclusive))))
                throw new ArgumentException(DomainMessageResources.RangeBoundsInvalid,
                    nameof(minimum));

            return new FieldRangeRule<T>(field, record =>
            {
                var value = selector(record);
                return (minimum.HasValue &&
                        (value < minimum.Value ||
                         (!minimumInclusive && value == minimum.Value))) ||
                       (maximum.HasValue &&
                        (value > maximum.Value ||
                         (!maximumInclusive && value == maximum.Value)));
            });
        }

        public static FieldRangeRule<T> Date(string field, Func<T, DateTime> selector,
            DateTime? minimum, bool minimumInclusive, DateTime? maximum, bool maximumInclusive)
        {
            ValidateFieldAndSelector(field, selector);
            if (!minimum.HasValue && !maximum.HasValue)
                throw new ArgumentException(DomainMessageResources.RangeBoundsInvalid,
                    nameof(minimum));
            if (minimum.HasValue && maximum.HasValue &&
                (DateTime.Compare(minimum.Value, maximum.Value) > 0 ||
                 (DateTime.Compare(minimum.Value, maximum.Value) == 0 &&
                  (!minimumInclusive || !maximumInclusive))))
                throw new ArgumentException(DomainMessageResources.RangeBoundsInvalid,
                    nameof(minimum));

            return new FieldRangeRule<T>(field, record =>
            {
                var value = selector(record);
                return (minimum.HasValue &&
                        (DateTime.Compare(value, minimum.Value) < 0 ||
                         (!minimumInclusive &&
                          DateTime.Compare(value, minimum.Value) == 0))) ||
                       (maximum.HasValue &&
                        (DateTime.Compare(value, maximum.Value) > 0 ||
                         (!maximumInclusive &&
                          DateTime.Compare(value, maximum.Value) == 0)));
            });
        }

        /// <summary>
        /// Returns no diagnostic for an in-range value. Out-of-range records have
        /// stable Domain diagnostics with source field and logical/physical location.
        /// </summary>
        public ImportDiagnostic Validate(T record, int row, int? physicalLine = null)
        {
            if (row < 1)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));
            return _outsideRange(record)
                ? ImportDiagnostic.ForRecord(row, Field,
                    DiagnosticCodes.FieldRangeOutOfRange, null, null, physicalLine)
                : null;
        }

        private static void ValidateFieldAndSelector<TValue>(
            string field, Func<T, TValue> selector)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.RangeFieldName,
                    nameof(field));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
        }
    }
}
