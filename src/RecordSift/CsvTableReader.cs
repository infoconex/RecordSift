using System;
using System.Collections.Generic;
using System.IO;

namespace RecordSift
{
    /// <summary>
    /// Applies an explicit CSV header policy to raw logical source records, using
    /// the shared FR-009/FR-013 schema binding. Does not map CLR properties.
    /// </summary>
    public static class CsvTableReader
    {
        public static CsvTableResult Read(TextReader source, int maxCharacters,
            ImportSchema schema, CsvHeaderPolicy policy,
            IEnumerable<SourceColumnPosition> positions = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (policy != CsvHeaderPolicy.WithHeader && policy != CsvHeaderPolicy.WithoutHeader)
                throw new ArgumentOutOfRangeException(nameof(policy));
            if (policy == CsvHeaderPolicy.WithHeader && positions != null)
                throw new ArgumentException("Headed sources use named header bindings, not positions.",
                    nameof(positions));

            var raw = CsvRecordReader.Read(source, maxCharacters);
            if (!raw.IsSuccessful)
                return CsvTableResult.Failed(raw.Diagnostics);

            // A headerless zero-byte file is a completed source with no records.
            // No positional binding can be checked without an actual source record.
            if (raw.Records.Count == 0)
                return policy == CsvHeaderPolicy.WithHeader
                    ? CsvTableResult.Failed(new[] {
                        ImportDiagnostic.ForSource(null, DiagnosticCodes.RequiredHeaderMissing)
                    })
                    : CsvTableResult.Completed(new CsvDataRow[0], new SourceColumnBinding[0]);

            SchemaBindingResult bound;
            var dataStart = 0;
            if (policy == CsvHeaderPolicy.WithHeader)
            {
                bound = schema.BindHeaders(raw.Records[0].Fields);
                dataStart = 1;
            }
            else
            {
                if (positions == null)
                    throw new ArgumentException(
                        "Nonempty headerless CSV requires explicit positional mappings.", nameof(positions));
                bound = schema.BindPositions(raw.Records[0].Fields.Count, positions);
            }

            if (!bound.IsSuccessful)
                return CsvTableResult.Failed(bound.Diagnostics);

            var rows = new List<CsvDataRow>();
            for (var i = dataStart; i < raw.Records.Count; i++)
            {
                var sourceRow = raw.Records[i];
                foreach (var mapping in bound.Bindings)
                {
                    if (mapping.SourceIndex >= sourceRow.Fields.Count)
                        return CsvTableResult.Failed(new[] {
                            ImportDiagnostic.ForSource(mapping.Name,
                                DiagnosticCodes.InvalidColumnPosition,
                                null, null, sourceRow.PhysicalLine)
                        });
                }

                rows.Add(new CsvDataRow(rows.Count + 1, sourceRow));
            }

            return CsvTableResult.Completed(rows, bound.Bindings);
        }
    }
}
