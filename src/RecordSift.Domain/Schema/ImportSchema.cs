using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Reusable schema for named source columns. Does not parse files, map record
    /// properties or perform type conversion. Explicit value requirements are validated separately.
    /// </summary>
    public sealed class ImportSchema
    {
        private readonly IReadOnlyList<FieldPatternRule> _patterns;

        /// <summary>Creates a reusable, ordinal, read-only snapshot of declared columns; duplicate names and null declarations are invalid.</summary>
        /// <param name="columns">Column declarations; null, duplicate names and null items are invalid.</param>
        public ImportSchema(IEnumerable<ExpectedColumn> columns)
            : this(columns, null)
        {
        }

        /// <summary>Creates a canonical source schema with optional, bounded, prevalidated regular-expression field rules.</summary>
        /// <param name="columns">Declared canonical columns and aliases; duplicates are invalid.</param>
        /// <param name="patternRules">Optional regex rules keyed by exact-case canonical column names; unknown, duplicate and null rules are invalid.</param>
        public ImportSchema(IEnumerable<ExpectedColumn> columns,
            IEnumerable<FieldPatternRule> patternRules)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            var snapshot = new List<ExpectedColumn>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in columns)
            {
                if (column == null)
                    throw new ArgumentException(DomainMessageResources.ColumnDeclarationNull, nameof(columns));
                if (!names.Add(column.Name))
                    throw new ArgumentException(DomainMessageResources.ColumnNamesDuplicate,
                        nameof(columns));
                foreach (var alias in column.Aliases)
                    if (!names.Add(alias))
                        throw new ArgumentException(DomainMessageResources.ColumnNamesDuplicate,
                            nameof(columns));
                snapshot.Add(column);
            }
            Columns = new ReadOnlyCollection<ExpectedColumn>(snapshot);

            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in snapshot)
                declared.Add(column.Name);
            var rules = new List<FieldPatternRule>();
            var patternFields = new HashSet<string>(StringComparer.Ordinal);
            if (patternRules != null)
                foreach (var rule in patternRules)
                {
                    if (rule == null)
                        throw new ArgumentException(
                            DomainMessageResources.PatternRuleNull, nameof(patternRules));
                    if (!declared.Contains(rule.Field))
                        throw new ArgumentException(
                            DomainMessageResources.PatternRuleUndeclared, nameof(patternRules));
                    if (!patternFields.Add(rule.Field))
                        throw new ArgumentException(
                            DomainMessageResources.PatternRuleDuplicate, nameof(patternRules));
                    rules.Add(rule);
                }
            _patterns = new ReadOnlyCollection<FieldPatternRule>(rules);
        }

        /// <summary>Gets declared columns in their original order as a read-only snapshot.</summary>
        public IReadOnlyList<ExpectedColumn> Columns { get; private set; }

        /// <summary>
        /// Validates present, decoded source text against explicitly declared bounded regex rules.
        /// Optional absent or null fields are skipped; normal IsMatch substring semantics apply.
        /// Any timeout or mismatch produces a field-specific, sanitized record diagnostic.
        /// </summary>
        /// <param name="values">Canonical, exact-case string fields for one record.</param>
        /// <param name="row">One-based logical record number.</param>
        /// <param name="physicalLine">Optional one-based source line for accurate provenance.</param>
        public IReadOnlyList<ImportDiagnostic> ValidatePatterns(
            IReadOnlyDictionary<string, string> values, int row, int? physicalLine = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));

            var sourceValues = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var value in values)
                sourceValues.Add(value.Key, value.Value);

            var errors = new List<ImportDiagnostic>();
            foreach (var rule in _patterns)
            {
                string value;
                if (!sourceValues.TryGetValue(rule.Field, out value) || value == null)
                    continue;
                if (!rule.Matches(value))
                    errors.Add(ImportDiagnostic.ForRecord(row, rule.Field,
                        DiagnosticCodes.ValidationFailed, null, null, physicalLine));
            }
            return new ReadOnlyCollection<ImportDiagnostic>(errors);
        }

        /// <summary>
        /// Validates opt-in required record values, preserving the original
        /// source field, logical row and reliable physical line. Source-column
        /// presence remains governed by BindHeaders or BindPositions.
        /// </summary>
        public IReadOnlyList<ImportDiagnostic> ValidateRequiredValues(
            IReadOnlyDictionary<string, string> values, int row, int? physicalLine = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));

            // The supplied dictionary's comparer cannot relax schema identifiers.
            var sourceValues = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in values)
                sourceValues.Add(pair.Key, pair.Value);

            var errors = new List<ImportDiagnostic>();
            foreach (var column in Columns)
            {
                if (!column.IsValueRequired)
                    continue;

                string value;
                if (!sourceValues.TryGetValue(column.Name, out value))
                {
                    if (column.IsRequired)
                        errors.Add(ImportDiagnostic.ForRecord(row, column.Name,
                            DiagnosticCodes.RequiredFieldMissing, null, null, physicalLine));
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add(ImportDiagnostic.ForRecord(row, column.Name,
                        DiagnosticCodes.RequiredFieldMissing, null, null, physicalLine));
                }
            }

            return new ReadOnlyCollection<ImportDiagnostic>(errors);
        }

        /// <summary>
        /// Validates explicitly declared inclusive bounds for present string values.
        /// Length counts untrimmed UTF-16 code units, independently of source format.
        /// Absent or null optional values are not made required by length constraints.
        /// </summary>
        public IReadOnlyList<ImportDiagnostic> ValidateFieldLengths(
            IReadOnlyDictionary<string, string> values, int row, int? physicalLine = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));

            // Preserve ordinal source identifiers even if the caller uses another comparer.
            var sourceValues = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in values)
                sourceValues.Add(pair.Key, pair.Value);

            var errors = new List<ImportDiagnostic>();
            foreach (var column in Columns)
            {
                if (!column.MinimumLength.HasValue && !column.MaximumLength.HasValue)
                    continue;

                string value;
                if (!sourceValues.TryGetValue(column.Name, out value) || value == null)
                    continue;

                var length = value.Length;
                if ((column.MinimumLength.HasValue && length < column.MinimumLength.Value) ||
                    (column.MaximumLength.HasValue && length > column.MaximumLength.Value))
                    errors.Add(ImportDiagnostic.ForRecord(row, column.Name,
                        DiagnosticCodes.FieldLengthOutOfRange, null, null, physicalLine));
            }

            return new ReadOnlyCollection<ImportDiagnostic>(errors);
        }

        /// <summary>
        /// Bind explicit zero-based source field positions for a source with the supplied
        /// field count. Reject ambiguous positions and all out-of-range declarations.
        /// This does not access a CSV/XLSX reader or materialize typed objects.
        /// </summary>
        public SchemaBindingResult BindPositions(int sourceFieldCount,
            IEnumerable<SourceColumnPosition> positions)
        {
            if (sourceFieldCount < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceFieldCount));
            if (positions == null)
                throw new ArgumentNullException(nameof(positions));

            var declarations = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in Columns)
                declarations.Add(column.Name);

            var mappings = new Dictionary<string, int>(StringComparer.Ordinal);
            var usedIndexes = new Dictionary<int, int>();
            foreach (var mapping in positions)
            {
                if (mapping == null)
                    throw new ArgumentException(DomainMessageResources.PositionMappingNull, nameof(positions));
                if (!declarations.Contains(mapping.Name))
                    throw new ArgumentException(DomainMessageResources.PositionMappingUndeclared,
                        nameof(positions));
                if (mappings.ContainsKey(mapping.Name))
                    throw new ArgumentException(DomainMessageResources.PositionMappingDuplicate,
                        nameof(positions));
                mappings.Add(mapping.Name, mapping.SourceIndex);

                int count;
                usedIndexes.TryGetValue(mapping.SourceIndex, out count);
                usedIndexes[mapping.SourceIndex] = count + 1;
            }

            var foundBindings = new List<SourceColumnBinding>();
            var errors = new List<ImportDiagnostic>();
            foreach (var column in Columns)
            {
                int index;
                if (!mappings.TryGetValue(column.Name, out index))
                {
                    if (column.IsRequired)
                        errors.Add(ImportDiagnostic.ForSource(column.Name,
                            DiagnosticCodes.RequiredColumnMissing));
                }
                else if (index >= sourceFieldCount)
                {
                    errors.Add(ImportDiagnostic.ForSource(column.Name,
                        DiagnosticCodes.InvalidColumnPosition));
                }
                else if (usedIndexes[index] != 1)
                {
                    errors.Add(ImportDiagnostic.ForSource(column.Name,
                        DiagnosticCodes.AmbiguousColumn));
                }
                else
                {
                    foundBindings.Add(new SourceColumnBinding(column.Name, index));
                }
            }

            return new SchemaBindingResult(foundBindings, errors);
        }

        /// <summary>Binds canonical or explicitly aliased exact-case headings, preserving declaration order and zero-based source indexes; multiple matching names fail atomically.</summary>
        /// <param name="sourceHeaders">Header field names in physical source order; matching is ordinal and case-sensitive.</param>
        public SchemaBindingResult BindHeaders(IEnumerable<string> sourceHeaders)
        {
            if (sourceHeaders == null) throw new ArgumentNullException(nameof(sourceHeaders));

            var positions = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var index = 0;
            foreach (var header in sourceHeaders)
            {
                if (!string.IsNullOrWhiteSpace(header))
                {
                    List<int> found;
                    if (!positions.TryGetValue(header, out found))
                    {
                        found = new List<int>();
                        positions.Add(header, found);
                    }
                    found.Add(index);
                }
                index++;
            }

            var foundBindings = new List<SourceColumnBinding>();
            var errors = new List<ImportDiagnostic>();
            foreach (var declared in Columns)
            {
                var matches = new List<int>();
                List<int> found;
                if (positions.TryGetValue(declared.Name, out found))
                    matches.AddRange(found);
                foreach (var alias in declared.Aliases)
                    if (positions.TryGetValue(alias, out found))
                        matches.AddRange(found);

                if (matches.Count == 0)
                {
                    if (declared.IsRequired)
                        errors.Add(ImportDiagnostic.ForSource(declared.Name,
                            DiagnosticCodes.RequiredColumnMissing));
                }
                else if (matches.Count != 1)
                {
                    // A canonical heading has no precedence over a configured alias.
                    // Even equal record values cannot make two source columns unambiguous.
                    errors.Add(ImportDiagnostic.ForSource(declared.Name,
                        DiagnosticCodes.AmbiguousColumn));
                }
                else
                {
                    // Use the canonical field identifier for all downstream mapping and validation.
                    foundBindings.Add(new SourceColumnBinding(declared.Name, matches[0]));
                }
            }

            return new SchemaBindingResult(foundBindings, errors);
        }
    }
}
