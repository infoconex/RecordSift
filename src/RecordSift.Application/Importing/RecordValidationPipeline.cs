using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Importing
{
    /// <summary>
    /// One format-independent validation and results pipeline for imported logical rows.
    /// Source parsing, header selection and binding are completed before entry.
    /// </summary>
    public static class RecordValidationPipeline
    {
        public static TypedImportResult<T> Process<T>(
            IReadOnlyList<ImportDataRow> rows,
            IReadOnlyList<SourceColumnBinding> bindings,
            ImportSchema schema, string sourceId,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules,
            DuplicateIdentifierRule<T> identifierRule,
            IEnumerable<RecordValidationRule<T>> customRules,
            string emptyMessage, string completedMessage, string rejectedMessage)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (mapRecord == null) throw new ArgumentNullException(nameof(mapRecord));

            var accepted = new List<T>();
            var diagnostics = new List<ImportDiagnostic>();
            var checkDuplicate = identifierRule == null
                ? null : identifierRule.CreateValidator();

            foreach (var row in rows)
            {
                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var binding in bindings)
                    values.Add(binding.Name, row.Fields[binding.SourceIndex]);

                var recordValues = new ReadOnlyDictionary<string, string>(values);
                var requiredErrors = schema.ValidateRequiredValues(
                    recordValues, row.Row, row.PhysicalLine);
                if (requiredErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, requiredErrors, row);
                    continue;
                }

                var lengthErrors = schema.ValidateFieldLengths(
                    recordValues, row.Row, row.PhysicalLine);
                if (lengthErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, lengthErrors, row);
                    continue;
                }

                T record;
                try
                {
                    record = mapRecord(recordValues);
                }
                catch (FieldConversionException error)
                {
                    diagnostics.Add(ForRow(row, error.Field, DiagnosticCodes.ConversionFailed));
                    continue;
                }
                catch (FormatException)
                {
                    diagnostics.Add(ForRow(row, null, DiagnosticCodes.ConversionFailed));
                    continue;
                }
                catch (OverflowException)
                {
                    diagnostics.Add(ForRow(row, null, DiagnosticCodes.ConversionFailed));
                    continue;
                }

                var rangeErrors = new List<ImportDiagnostic>();
                if (rangeRules != null)
                    foreach (var rule in rangeRules)
                    {
                        var error = rule.Validate(record, row.Row, row.PhysicalLine);
                        if (error != null) rangeErrors.Add(error);
                    }
                if (rangeErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, rangeErrors, row);
                    continue;
                }

                var customErrors = new List<ImportDiagnostic>();
                if (customRules != null)
                    foreach (var rule in customRules)
                    {
                        var error = rule.Validate(record, row.Row, row.PhysicalLine);
                        if (error != null) customErrors.Add(error);
                    }
                if (customErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, customErrors, row);
                    continue;
                }

                var duplicate = checkDuplicate == null
                    ? null : checkDuplicate(record, row.Row, row.PhysicalLine);
                if (duplicate != null)
                    diagnostics.Add(WithProvenance(duplicate, row));
                else
                    accepted.Add(record);
            }

            var message = rows.Count == 0 ? emptyMessage
                : diagnostics.Count == 0 ? completedMessage : rejectedMessage;
            return new TypedImportResult<T>(sourceId,
                ImportResult.Completed(rows.Count, diagnostics), accepted, message);
        }

        private static ImportDiagnostic ForRow(ImportDataRow row, string field, string code)
        {
            return ImportDiagnostic.ForRecord(row.Row, field, code,
                row.Worksheet, row.PhysicalRow, row.PhysicalLine);
        }

        private static ImportDiagnostic WithProvenance(ImportDiagnostic diagnostic,
            ImportDataRow row)
        {
            return ForRow(row, diagnostic.Field, diagnostic.Code);
        }

        private static void AppendDiagnostics(List<ImportDiagnostic> results,
            IEnumerable<ImportDiagnostic> errors, ImportDataRow row)
        {
            foreach (var error in errors)
                results.Add(WithProvenance(error, row));
        }
    }
}
