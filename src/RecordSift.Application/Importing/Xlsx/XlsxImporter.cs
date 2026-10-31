using System;
using System.Collections.Generic;
using System.IO;
using RecordSift.Application.Importing;
using RecordSift.Application.Messages;
using RecordSift.Application.Ports.Xlsx;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Importing.Xlsx
{
    /// <summary>Imports a selected or default XLSX worksheet through shared validation.</summary>
    /// <typeparam name="T">CLR record type constructed or processed by this service.</typeparam>
    public sealed class XlsxImporter<T>
    {
        private readonly IXlsxWorksheetReader _reader;
        private readonly Func<IReadOnlyDictionary<string, string>, T> _mapRecord;
        private readonly IReadOnlyList<FieldRangeRule<T>> _rangeRules;
        private readonly DuplicateIdentifierRule<T> _identifierRule;
        private readonly IReadOnlyList<RecordValidationRule<T>> _customRules;

        /// <summary>Constructs a worksheet importer using a replaceable reader and mapper; optional validation declarations are snapshotted.</summary>
        /// <param name="reader">Replaceable source port implemented by infrastructure.</param>
        /// <param name="mapRecord">Maps exact-case input string fields into a CLR record.</param>
        /// <param name="rangeRules">Optional range checks for already-converted records.</param>
        /// <param name="identifierRule">Optional duplicate-key rule with independent validator state per import.</param>
        /// <param name="customRules">Optional custom predicates evaluated after conversion and range checks.</param>
        public XlsxImporter(IXlsxWorksheetReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules = null,
            DuplicateIdentifierRule<T> identifierRule = null,
            IEnumerable<RecordValidationRule<T>> customRules = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapRecord == null) throw new ArgumentNullException(nameof(mapRecord));
            _reader = reader;
            _mapRecord = mapRecord;
            _rangeRules = Snapshot(rangeRules);
            _identifierRule = identifierRule;
            _customRules = Snapshot(customRules);
        }

        /// <summary>Imports a named or first worksheet from a caller-owned byte stream; source failures return an atomic failed result, while invalid arguments throw.</summary>
        /// <param name="source">Caller-owned stream or reader; implementations do not dispose it.</param>
        /// <param name="maxBytes">Positive maximum raw XLSX stream length in bytes, before decompression.</param>
        /// <param name="schema">Declared source field schema used for header matching.</param>
        /// <param name="sourceId">Nonblank opaque source identifier supplied by the caller.</param>
        /// <param name="limits">Optional worksheet row, column and cell-character limits; null selects defaults.</param>
        /// <param name="worksheetName">Optional exact-case worksheet name; null selects the first worksheet.</param>
        /// <remarks>Headers come from the first nonblank source row; no rows produces a missing-header source failure, while a header-only worksheet completes with zero data records. Logical diagnostic rows are one-based data-row ordinals excluding the header; physical worksheet row locations are retained separately. The source stream remains open.</remarks>
        public TypedImportResult<T> Import(Stream source, int maxBytes,
            ImportSchema schema, string sourceId, XlsxReadLimits limits = null,
            string worksheetName = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxBytes));
            if (worksheetName != null && string.IsNullOrWhiteSpace(worksheetName))
                throw new ArgumentException(DomainMessageResources.DiagnosticWorksheetBlank,
                    nameof(worksheetName));

            // Bounded input and row/cell limits are explicitly independent of CSV.
            var budget = limits ?? XlsxReadLimits.Default;
            var raw = _reader.Read(source, maxBytes,
                budget.MaxRows, budget.MaxColumns, budget.MaxCellCharacters, worksheetName);
            if (!raw.IsSuccessful)
                return Failed(sourceId, raw.Error);

            if (raw.Records.Count == 0)
                return Failed(sourceId, ImportDiagnostic.ForSource(null,
                    DiagnosticCodes.RequiredHeaderMissing, raw.Worksheet, null, null));

            var binding = schema.BindHeaders(raw.Records[0].Fields);
            if (!binding.IsSuccessful)
                return Failed(sourceId, binding.Diagnostics);

            var rows = new List<ImportDataRow>();
            for (var i = 1; i < raw.Records.Count; i++)
            {
                var record = raw.Records[i];
                foreach (var column in binding.Bindings)
                    if (column.SourceIndex >= record.Fields.Count)
                        return Failed(sourceId, ImportDiagnostic.ForSource(column.Name,
                            DiagnosticCodes.InvalidColumnPosition, raw.Worksheet, record.PhysicalRow, null));

                rows.Add(new ImportDataRow(rows.Count + 1, record.Fields,
                    raw.Worksheet, record.PhysicalRow));
            }
            return RecordValidationPipeline.Process(rows, binding.Bindings, schema,
                sourceId, _mapRecord, _rangeRules, _identifierRule, _customRules,
                CsvMessageResources.ImportNoDataRecords, CsvMessageResources.ImportCompleted,
                CsvMessageResources.ImportCompletedWithErrors);
        }

        private static TypedImportResult<T> Failed(string sourceId, ImportDiagnostic error)
        {
            return Failed(sourceId, new[] { error });
        }

        private static TypedImportResult<T> Failed(string sourceId,
            IEnumerable<ImportDiagnostic> errors)
        {
            return new TypedImportResult<T>(sourceId, ImportResult.Failed(0, errors),
                new T[0], CsvMessageResources.ImportFailed);
        }

        private static IReadOnlyList<TRule> Snapshot<TRule>(IEnumerable<TRule> rules)
            where TRule : class
        {
            var result = new List<TRule>();
            if (rules != null)
                foreach (var rule in rules)
                {
                    if (rule == null) throw new ArgumentException(nameof(rules));
                    result.Add(rule);
                }
            return result.AsReadOnly();
        }
    }
}
