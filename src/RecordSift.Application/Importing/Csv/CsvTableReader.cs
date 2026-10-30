using System;
using System.Collections.Generic;
using System.IO;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Importing.Csv
{
    /// <summary>
    /// Applies an explicit CSV header policy to raw logical source records, using
    /// the shared FR-009/FR-013 schema binding. Does not map CLR properties.
    /// </summary>
    public static class CsvTableReader
    {
        public static CsvTableResult Read(ICsvRecordReader reader, TextReader source, int maxCharacters,
            ImportSchema schema, CsvHeaderPolicy policy,
            IEnumerable<SourceColumnPosition> positions = null, CsvReadOptions options = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (policy != CsvHeaderPolicy.WithHeader && policy != CsvHeaderPolicy.WithoutHeader)
                throw new ArgumentOutOfRangeException(nameof(policy));
            if (policy == CsvHeaderPolicy.WithHeader && positions != null)
                throw new ArgumentException(DomainMessageResources.Get("Argument_ReaderHeaderConflict"),
                    nameof(positions));

            if (options != null && !options.IsValid)
                return CsvTableResult.Failed(new[] {
                    ImportDiagnostic.ForSource(null, DiagnosticCodes.InvalidCsvDelimiter)
                });

            var raw = reader.Read(source, maxCharacters, options);
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
                        DomainMessageResources.Get("Argument_HeaderlessMissingPositions"), nameof(positions));
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
