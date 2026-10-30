using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Reusable schema for named source columns. Does not parse files, map record
    /// properties, validate cell values or perform future positional mapping.
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
                    throw new ArgumentException(DomainMessageResources.Get("Argument_ColumnDeclarationNull"), nameof(columns));
                if (!names.Add(column.Name))
                    throw new ArgumentException(DomainMessageResources.Get("Argument_ColumnNamesDuplicate"),
                        nameof(columns));
                snapshot.Add(column);
            }
            Columns = new ReadOnlyCollection<ExpectedColumn>(snapshot);
        }

        public IReadOnlyList<ExpectedColumn> Columns { get; private set; }

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
                    throw new ArgumentException(DomainMessageResources.Get("Argument_PositionMappingNull"), nameof(positions));
                if (!declarations.Contains(mapping.Name))
                    throw new ArgumentException(DomainMessageResources.Get("Argument_PositionMappingUndeclared"),
                        nameof(positions));
                if (mappings.ContainsKey(mapping.Name))
                    throw new ArgumentException(DomainMessageResources.Get("Argument_PositionMappingDuplicate"),
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
