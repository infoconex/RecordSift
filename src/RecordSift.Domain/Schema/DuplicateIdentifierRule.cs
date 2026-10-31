using System;
using System.Collections.Generic;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Immutable typed identifier policy. Each import creates an independent
    /// validator that remembers only records accepted by its caller.
    /// </summary>
    public sealed class DuplicateIdentifierRule<T>
    {
        private readonly Func<Func<T, int, int?, ImportDiagnostic>> _createValidator;

        private DuplicateIdentifierRule(string field,
            Func<Func<T, int, int?, ImportDiagnostic>> createValidator)
        {
            Field = field;
            _createValidator = createValidator;
        }

        /// <summary>Gets the source field reported for a repeated identifier.</summary>
        public string Field { get; private set; }

        /// <summary>
        /// Uses the supplied typed equality policy, or EqualityComparer{TKey}.Default.
        /// Null keys are not implicitly required and are not tracked as duplicates.
        /// </summary>
        public static DuplicateIdentifierRule<T> ForKey<TKey>(
            string field, Func<T, TKey> selector, IEqualityComparer<TKey> comparer = null)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DuplicateFieldName,
                    nameof(field));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return new DuplicateIdentifierRule<T>(field, () =>
            {
                var seen = new HashSet<TKey>(comparer ?? EqualityComparer<TKey>.Default);
                return (record, row, physicalLine) =>
                {
                    if (row < 1)
                        throw new ArgumentOutOfRangeException(nameof(row));
                    if (physicalLine.HasValue && physicalLine.Value < 1)
                        throw new ArgumentOutOfRangeException(nameof(physicalLine));

                    var identifier = selector(record);
                    if (object.Equals(identifier, null) || seen.Add(identifier))
                        return null;
                    return ImportDiagnostic.ForRecord(row, field,
                        DiagnosticCodes.DuplicateIdentifier, null, null, physicalLine);
                };
            });
        }

        /// <summary>
        /// Returns a new import-scoped validator. Call it only for records that
        /// have passed preceding type and field validations; the first accepted
        /// identifier is retained, later matches are rejected.
        /// </summary>
        public Func<T, int, int?, ImportDiagnostic> CreateValidator()
        {
            return _createValidator();
        }
    }
}
