using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using RecordSift.Application.Messages;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Importing.Csv
{
    /// <summary>
    /// Integrates CSV source/header/schema policy with an explicitly supplied
    /// CLR record converter. Generalized property mapping and rule validation
    /// are separate requirements; no vendor parser type is exposed.
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

            var accepted = new List<T>();
            var diagnostics = new List<ImportDiagnostic>();
            var checkDuplicate = _identifierRule == null
                ? null : _identifierRule.CreateValidator();
            foreach (var row in table.DataRows)
            {
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var binding in table.Bindings)
                    values.Add(binding.Name, row.Fields[binding.SourceIndex]);

                var recordValues = new ReadOnlyDictionary<string, string>(values);
                var requiredErrors = schema.ValidateRequiredValues(
                    recordValues, row.Row, row.PhysicalLine);
                if (requiredErrors.Count != 0)
                {
                    diagnostics.AddRange(requiredErrors);
                    continue;
                }

                var lengthErrors = schema.ValidateFieldLengths(
                    recordValues, row.Row, row.PhysicalLine);
                if (lengthErrors.Count != 0)
                {
                    diagnostics.AddRange(lengthErrors);
                    continue;
                }

                T record;
                try
                {
                    record = _mapRecord(recordValues);
                }
                catch (FieldConversionException error)
                {
                    diagnostics.Add(ImportDiagnostic.ForRecord(row.Row, error.Field,
                        DiagnosticCodes.ConversionFailed, null, null, row.PhysicalLine));
                    continue;
                }
                catch (FormatException)
                {
                    diagnostics.Add(ImportDiagnostic.ForRecord(row.Row, null,
                        DiagnosticCodes.ConversionFailed, null, null, row.PhysicalLine));
                    continue;
                }
                catch (OverflowException)
                {
                    diagnostics.Add(ImportDiagnostic.ForRecord(row.Row, null,
                        DiagnosticCodes.ConversionFailed, null, null, row.PhysicalLine));
                    continue;
                }

                var rangeErrors = new List<ImportDiagnostic>();
                foreach (var rule in _rangeRules)
                {
                    var diagnostic = rule.Validate(record, row.Row, row.PhysicalLine);
                    if (diagnostic != null)
                        rangeErrors.Add(diagnostic);
                }
                if (rangeErrors.Count != 0)
                {
                    diagnostics.AddRange(rangeErrors);
                    continue;
                }

                var customErrors = new List<ImportDiagnostic>();
                foreach (var rule in _customRules)
                {
                    var diagnostic = rule.Validate(record, row.Row, row.PhysicalLine);
                    if (diagnostic != null)
                        customErrors.Add(diagnostic);
                }
                if (customErrors.Count != 0)
                {
                    diagnostics.AddRange(customErrors);
                    continue;
                }

                var duplicate = checkDuplicate == null
                    ? null : checkDuplicate(record, row.Row, row.PhysicalLine);
                if (duplicate != null)
                    diagnostics.Add(duplicate);
                else
                    accepted.Add(record);
            }

            string summaryMessage;
            if (table.DataRows.Count == 0)
            {
                summaryMessage = policy == CsvHeaderPolicy.WithHeader
                    ? CsvMessageResources.ImportNoDataRecords
                    : CsvMessageResources.ImportEmptySource;
            }
            else
            {
                summaryMessage = diagnostics.Count == 0
                    ? CsvMessageResources.ImportCompleted
                    : CsvMessageResources.ImportCompletedWithErrors;
            }

            return new TypedImportResult<T>(sourceId,
                ImportResult.Completed(table.DataRows.Count, diagnostics),
                accepted, summaryMessage);
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
