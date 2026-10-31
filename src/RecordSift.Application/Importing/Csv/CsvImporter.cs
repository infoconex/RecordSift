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
    /// <typeparam name="T">Type of accepted mapped CLR records.</typeparam>
    public sealed class CsvImporter<T>
    {
        private readonly ICsvRecordReader _reader;
        private readonly Func<IReadOnlyDictionary<string, string>, T> _mapRecord;
        private readonly IReadOnlyList<FieldRangeRule<T>> _rangeRules;
        private readonly DuplicateIdentifierRule<T> _identifierRule;
        private readonly IReadOnlyList<RecordValidationRule<T>> _customRules;

        /// <summary>Creates a CSV import orchestrator with a replaceable reader, required mapper, and optional validation rules snapshotted per importer.</summary>
        /// <param name="reader">Replaceable reader owned by the caller; vendor types are not exposed.</param>
        /// <param name="mapRecord">Function mapping exact-case source fields to a CLR record; programming exceptions propagate.</param>
        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord)
            : this(reader, mapRecord, null)
        {
        }

        /// <summary>Creates a CSV import orchestrator with a replaceable reader, required mapper, and optional validation rules snapshotted per importer.</summary>
        /// <param name="reader">Replaceable reader owned by the caller; vendor types are not exposed.</param>
        /// <param name="mapRecord">Function mapping exact-case source fields to a CLR record; programming exceptions propagate.</param>
        /// <param name="rangeRules">Optional range rules; declarations are snapshotted and null entries rejected.</param>
        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules)
            : this(reader, mapRecord, rangeRules, null)
        {
        }

        /// <summary>Creates a CSV import orchestrator with a replaceable reader, required mapper, and optional validation rules snapshotted per importer.</summary>
        /// <param name="reader">Replaceable reader owned by the caller; vendor types are not exposed.</param>
        /// <param name="mapRecord">Function mapping exact-case source fields to a CLR record; programming exceptions propagate.</param>
        /// <param name="rangeRules">Optional range rules; declarations are snapshotted and null entries rejected.</param>
        /// <param name="identifierRule">Optional duplicate identifier policy with independent state per import.</param>
        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules,
            DuplicateIdentifierRule<T> identifierRule)
            : this(reader, mapRecord, rangeRules, identifierRule, null)
        {
        }

        /// <summary>Creates a CSV import orchestrator with a replaceable reader, required mapper, and optional validation rules snapshotted per importer.</summary>
        /// <param name="reader">Replaceable reader owned by the caller; vendor types are not exposed.</param>
        /// <param name="mapRecord">Function mapping exact-case source fields to a CLR record; programming exceptions propagate.</param>
        /// <param name="rangeRules">Optional range rules; declarations are snapshotted and null entries rejected.</param>
        /// <param name="identifierRule">Optional duplicate identifier policy with independent state per import.</param>
        /// <param name="customRules">Optional custom predicates; declarations are snapshotted and null entries rejected.</param>
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

        /// <summary>Imports caller-owned, already-decoded CSV text with an explicit character budget and header policy; invalid arguments throw, source errors return an atomic failed result without accepted records.</summary>
        /// <param name="source">Caller-owned TextReader, which is not disposed by this importer or reader.</param>
        /// <param name="maxCharacters">Positive maximum number of decoded UTF-16 characters the CSV reader may consume.</param>
        /// <param name="schema">Declared source columns and required-value constraints.</param>
        /// <param name="policy">Explicit header policy, not inferred from content.</param>
        /// <param name="sourceId">Nonblank caller-provided opaque source identifier.</param>
        /// <param name="positions">Required explicit zero-based indexes for nonempty headerless CSV; incompatible with header mode.</param>
        /// <param name="options">Optional parser options; null selects comma delimiter.</param>
        /// <remarks>Source decoding and byte encoding are the caller's responsibility; use the CSV stream adapter for explicit encoding. A header-only CSV completes with zero data rows, but a zero-record source with a required header fails with a source diagnostic. A headerless zero-record source completes without validating positional bindings. Source-level failures return no accepted records, while record validation failures reject only the affected rows.</remarks>
        /// <returns>A typed result with read-only accepted records and structured source/record diagnostics.</returns>
        public TypedImportResult<T> Import(TextReader source, int maxCharacters,
            ImportSchema schema, CsvHeaderPolicy policy, string sourceId,
            IEnumerable<SourceColumnPosition> positions = null,
            CsvReadOptions options = null)
        {
            return Import(source, maxCharacters, schema, policy, sourceId,
                positions, options, false);
        }

        /// <summary>Imports CSV with an explicit optional fail-fast policy, stopping validation of later rows after the first rejected row.</summary>
        /// <param name="source">Caller-owned text reader.</param>
        /// <param name="maxCharacters">Positive decoded character input budget.</param>
        /// <param name="schema">Declared source schema.</param>
        /// <param name="policy">Header interpretation.</param>
        /// <param name="sourceId">Opaque source identifier.</param>
        /// <param name="positions">Optional positional mappings for headerless input.</param>
        /// <param name="options">Optional parser configuration.</param>
        /// <param name="failFast">Opt-in to stop row validation on its first error; source parsing is still eager.</param>
        public TypedImportResult<T> Import(TextReader source, int maxCharacters,
            ImportSchema schema, CsvHeaderPolicy policy, string sourceId,
            IEnumerable<SourceColumnPosition> positions, CsvReadOptions options,
            bool failFast)
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
                CsvMessageResources.ImportCompletedWithErrors, failFast,
                CsvMessageResources.ImportStopped);
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
