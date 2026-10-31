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
        /// <summary>Applies required-value, length, conversion, range, custom and duplicate checks to already-bound logical data rows in that order; rejected rows are excluded from accepted records.</summary>
        /// <param name="rows">Already-parsed logical data rows in source order.</param>
        /// <param name="bindings">Resolved zero-based field indexes for declared columns.</param>
        /// <param name="schema">Declared source field schema used for header matching.</param>
        /// <param name="sourceId">Nonblank opaque source identifier supplied by the caller.</param>
        /// <param name="mapRecord">Maps exact-case input string fields into a CLR record.</param>
        /// <param name="rangeRules">Optional range checks for already-converted records.</param>
        /// <param name="identifierRule">Optional duplicate-key rule with independent validator state per import.</param>
        /// <param name="customRules">Optional custom predicates evaluated after conversion and range checks.</param>
        /// <param name="emptyMessage">Result text for an empty processed data set.</param>
        /// <param name="completedMessage">Result text when every record is accepted.</param>
        /// <param name="rejectedMessage">Result text when one or more records are rejected.</param>
        /// <typeparam name="T">Accepted CLR record type.</typeparam>
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

                // A missing optional source field may have an explicit default.
                // Never replace any bound value, even an empty or whitespace value.
                foreach (var column in schema.Columns)
                {
                    if (!column.IsRequired && column.DefaultValue != null &&
                        !values.ContainsKey(column.Name))
                        values.Add(column.Name, column.DefaultValue);
                }

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
