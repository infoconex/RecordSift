using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Caller-provided, format-neutral validation of an already-converted record.
    /// A null field identifies a whole-record rule. Failures use the registered,
    /// sanitized Domain validation diagnostic rather than arbitrary input text.
    /// </summary>
    /// <typeparam name="T">Type of already-converted records inspected by this rule.</typeparam>
    public sealed class RecordValidationRule<T>
    {
        private readonly Func<T, bool> _isValid;

        /// <summary>Creates a predicate-based record rule; null field identifies a record-wide error, while blank nonnull field is invalid.</summary>
        /// <param name="field">Source field named in any failure diagnostic.</param>
        /// <param name="isValid">Predicate returning true for an accepted record.</param>
        public RecordValidationRule(string field, Func<T, bool> isValid)
        {
            if (field != null && string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DiagnosticFieldBlank,
                    nameof(field));
            if (isValid == null)
                throw new ArgumentNullException(nameof(isValid));
            Field = field;
            _isValid = isValid;
        }

        /// <summary>Gets the optional source field associated with this rule, or null for whole-record checks.</summary>
        public string Field { get; private set; }

        /// <summary>
        /// Returns a safe diagnostic with caller-owned provenance, or null on success.
        /// Custom predicate exceptions remain programming failures and are not converted
        /// into misleading source or record-conversion diagnostics.
        /// </summary>
        public ImportDiagnostic Validate(T record, int row, int? physicalLine = null)
        {
            if (row < 1)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));

            return _isValid(record) ? null : ImportDiagnostic.ForRecord(
                row, Field, DiagnosticCodes.ValidationFailed, null, null, physicalLine);
        }
    }
}
