using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift
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
                    throw new ArgumentException("Column declarations cannot contain null.", nameof(columns));
                if (!names.Add(column.Name))
                    throw new ArgumentException("Declared column names must be unique (ordinal match).",
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
