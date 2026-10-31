using System;
using System.Text.RegularExpressions;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Format-independent regular-expression validation for one decoded source string.
    /// Every evaluation is bounded; matching uses ordinal case-sensitive culture-invariant semantics.
    /// </summary>
    public sealed class FieldPatternRule
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan s_maximumTimeout = TimeSpan.FromSeconds(10);
        private readonly Regex _expression;

        /// <summary>Creates a case-sensitive pattern rule with a finite 250-millisecond evaluation timeout.</summary>
        /// <param name="field">Exact-case canonical source field name.</param>
        /// <param name="pattern">Regular expression to evaluate using normal substring-matching semantics.</param>
        public FieldPatternRule(string field, string pattern)
            : this(field, pattern, s_defaultTimeout)
        {
        }

        /// <summary>Creates a case-sensitive pattern rule with an explicit finite evaluation timeout.</summary>
        /// <param name="field">Exact-case canonical source field name.</param>
        /// <param name="pattern">Expression to evaluate without implicit trimming or case-folding.</param>
        /// <param name="timeout">Finite match budget between one millisecond and ten seconds.</param>
        public FieldPatternRule(string field, string pattern, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DiagnosticFieldBlank, nameof(field));
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException(DomainMessageResources.PatternRuleInvalidExpression, nameof(pattern));
            if (timeout < TimeSpan.FromMilliseconds(1) || timeout > s_maximumTimeout)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            try
            {
                _expression = new Regex(pattern, RegexOptions.CultureInvariant, timeout);
            }
            catch (ArgumentException)
            {
                // A parser exception may embed the untrusted pattern; never preserve it.
                throw new ArgumentException(
                    DomainMessageResources.PatternRuleInvalidExpression, nameof(pattern));
            }

            Field = field;
        }

        /// <summary>Gets the canonical exact-case source field associated with the pattern.</summary>
        public string Field { get; private set; }

        internal bool Matches(string value)
        {
            try
            {
                return _expression.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                // Treat a bounded match timeout as a validation failure without
                // leaking pattern or source data to the diagnostic surface.
                return false;
            }
        }
    }
}
