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
        public ImportSchema(IEnumerable<ExpectedColumn> columns)
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
                snapshot.Add(column);
            }
            Columns = new ReadOnlyCollection<ExpectedColumn>(snapshot);
        }

        public IReadOnlyList<ExpectedColumn> Columns { get; private set; }

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
        /// Bind to a supplied sequence of source header names, retaining declaration
        /// order and zero-based source column indices. Invalid binding yields no partial
        /// mappings, only source-scope errors for declared columns.
        /// </summary>
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
                List<int> found;
                if (!positions.TryGetValue(declared.Name, out found))
                {
                    if (declared.IsRequired)
                        errors.Add(ImportDiagnostic.ForSource(declared.Name,
                            DiagnosticCodes.RequiredColumnMissing));
                }
                else if (found.Count != 1)
                {
                    errors.Add(ImportDiagnostic.ForSource(declared.Name,
                        DiagnosticCodes.AmbiguousColumn));
                }
                else
                {
                    foundBindings.Add(new SourceColumnBinding(declared.Name, found[0]));
                }
            }

            return new SchemaBindingResult(foundBindings, errors);
        }
    }
}
