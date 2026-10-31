using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using RecordSift.Application.Importing;
using RecordSift.Application.Messages;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Importing.Csv
{
    /// <summary>
    /// Integrates CSV source/header/schema policy with an explicitly supplied
    /// CLR record converter. Business validation is shared with XLSX and
    /// no vendor parser type is exposed.
    /// </summary>
    public sealed class CsvImporter<T>
    {
        private readonly ICsvRecordReader _reader;
        private readonly Func<IReadOnlyDictionary<string, string>, T> _mapRecord;
        private readonly IReadOnlyList<FieldRangeRule<T>> _rangeRules;
        private readonly DuplicateIdentifierRule<T> _identifierRule;
        private readonly IReadOnlyList<RecordValidationRule<T>> _customRules;

        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord)
            : this(reader, mapRecord, null)
        {
        }

        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules)
            : this(reader, mapRecord, rangeRules, null)
        {
        }

        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules,
            DuplicateIdentifierRule<T> identifierRule)
            : this(reader, mapRecord, rangeRules, identifierRule, null)
        {
        }

        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules,
            DuplicateIdentifierRule<T> identifierRule,
            IEnumerable<RecordValidationRule<T>> customRules)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapRecord == null) throw new ArgumentNullException(nameof(mapRecord));
            _reader = reader;
            _mapRecord = mapRecord;

            var rules = new List<FieldRangeRule<T>>();
            if (rangeRules != null)
            {
                foreach (var rule in rangeRules)
                {
                    if (rule == null)
                        throw new ArgumentException(DomainMessageResources.RangeRuleNull,
                            nameof(rangeRules));
                    rules.Add(rule);
                }
            }
            _rangeRules = new ReadOnlyCollection<FieldRangeRule<T>>(rules);
            _identifierRule = identifierRule;

            var validations = new List<RecordValidationRule<T>>();
            if (customRules != null)
            {
                foreach (var rule in customRules)
                {
                    if (rule == null)
                        throw new ArgumentException(DomainMessageResources.RecordValidationRuleNull,
                            nameof(customRules));
                    validations.Add(rule);
                }
            }
            _customRules = new ReadOnlyCollection<RecordValidationRule<T>>(validations);
        }

        public TypedImportResult<T> Import(TextReader source, int maxCharacters,
            ImportSchema schema, CsvHeaderPolicy policy, string sourceId,
            IEnumerable<SourceColumnPosition> positions = null,
            CsvReadOptions options = null)
        {
            var table = CsvTableReader.Read(_reader, source, maxCharacters,
                schema, policy, positions, options);
            if (table.Status == ImportStatus.Failed)
            {
                var message = ContainsMissingHeader(table.Diagnostics)
                    ? CsvMessageResources.ImportMissingHeader
                    : CsvMessageResources.ImportFailed;
                return new TypedImportResult<T>(sourceId,
                    ImportResult.Failed(0, table.Diagnostics), new T[0], message);
            }

            var rows = new List<ImportDataRow>();
            foreach (var row in table.DataRows)
                rows.Add(new ImportDataRow(row.Row, row.Fields, null, null, row.PhysicalLine));

            var emptyMessage = policy == CsvHeaderPolicy.WithHeader
                ? CsvMessageResources.ImportNoDataRecords
                : CsvMessageResources.ImportEmptySource;
            return RecordValidationPipeline.Process(rows, table.Bindings, schema,
                sourceId, _mapRecord, _rangeRules, _identifierRule, _customRules,
                emptyMessage, CsvMessageResources.ImportCompleted,
                CsvMessageResources.ImportCompletedWithErrors);
        }

        private static bool ContainsMissingHeader(IEnumerable<ImportDiagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Code == DiagnosticCodes.RequiredHeaderMissing)
                    return true;
            }
            return false;
        }
    }
}
