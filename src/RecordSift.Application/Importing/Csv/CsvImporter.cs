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

        public CsvImporter(ICsvRecordReader reader,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapRecord == null) throw new ArgumentNullException(nameof(mapRecord));
            _reader = reader;
            _mapRecord = mapRecord;
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
            foreach (var row in table.DataRows)
            {
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var binding in table.Bindings)
                    values.Add(binding.Name, row.Fields[binding.SourceIndex]);

                try
                {
                    accepted.Add(_mapRecord(new ReadOnlyDictionary<string, string>(values)));
                }
                catch (FormatException)
                {
                    diagnostics.Add(ImportDiagnostic.ForRecord(row.Row, null,
                        DiagnosticCodes.ConversionFailed, null, null, row.PhysicalLine));
                }
                catch (OverflowException)
                {
                    diagnostics.Add(ImportDiagnostic.ForRecord(row.Row, null,
                        DiagnosticCodes.ConversionFailed, null, null, row.PhysicalLine));
                }
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
