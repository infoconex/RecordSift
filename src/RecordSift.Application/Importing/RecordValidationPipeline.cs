using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
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
        /// <summary>Applies required-value, length, bounded pattern, conversion, range, custom and duplicate checks to already-bound logical data rows in that order; rejected rows are excluded from accepted records.</summary>
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
            return Process(rows, bindings, schema, sourceId, mapRecord, rangeRules,
                identifierRule, customRules, emptyMessage, completedMessage,
                rejectedMessage, false, null);
        }

        /// <summary>Validates logical rows with optional fail-fast stopping at the first record error, retaining the accepted prefix and reliable provenance.</summary>
        /// <param name="rows">Already-parsed logical rows.</param>
        /// <param name="bindings">Canonical source bindings.</param>
        /// <param name="schema">Source schema and field constraints.</param>
        /// <param name="sourceId">Opaque source identifier.</param>
        /// <param name="mapRecord">Typed mapper with documented conversion failure semantics.</param>
        /// <param name="rangeRules">Optional converted-value range rules.</param>
        /// <param name="identifierRule">Optional duplicate identifier policy.</param>
        /// <param name="customRules">Optional typed record predicates.</param>
        /// <param name="emptyMessage">Trusted empty import text.</param>
        /// <param name="completedMessage">Trusted successful completion text.</param>
        /// <param name="rejectedMessage">Trusted completed-with-errors text.</param>
        /// <param name="failFast">When true, stop validation at the first failed logical row.</param>
        /// <param name="stoppedMessage">Trusted stopped-state text; required when failFast is true.</param>
        /// <typeparam name="T">Accepted CLR record type.</typeparam>
        public static TypedImportResult<T> Process<T>(
            IReadOnlyList<ImportDataRow> rows,
            IReadOnlyList<SourceColumnBinding> bindings,
            ImportSchema schema, string sourceId,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules,
            DuplicateIdentifierRule<T> identifierRule,
            IEnumerable<RecordValidationRule<T>> customRules,
            string emptyMessage, string completedMessage, string rejectedMessage,
            bool failFast, string stoppedMessage)
        {
            return Process(rows, bindings, schema, sourceId, mapRecord, rangeRules,
                identifierRule, customRules, emptyMessage, completedMessage,
                rejectedMessage, failFast, stoppedMessage, CancellationToken.None, null);
        }

        /// <summary>Processes a source with optional fail-fast validation and cooperative cancellation at logical record checkpoints.</summary>
        /// <param name="rows">Already-parsed logical records.</param>
        /// <param name="bindings">Bound column indexes.</param>
        /// <param name="schema">Source schema.</param>
        /// <param name="sourceId">Opaque source identifier.</param>
        /// <param name="mapRecord">Converts bound fields to accepted CLR records.</param>
        /// <param name="rangeRules">Optional converted value constraints.</param>
        /// <param name="identifierRule">Optional duplicate-key constraint.</param>
        /// <param name="customRules">Optional typed predicates.</param>
        /// <param name="emptyMessage">Trusted empty import text.</param>
        /// <param name="completedMessage">Trusted success text.</param>
        /// <param name="rejectedMessage">Trusted rejected-row text.</param>
        /// <param name="failFast">Stop validation after the first record error.</param>
        /// <param name="stoppedMessage">Trusted fail-fast text when enabled.</param>
        /// <param name="cancellationToken">Caller-owned cooperative cancellation signal, checked at record boundaries.</param>
        /// <param name="canceledMessage">Trusted cancellation text, required for cancellable calls.</param>
        /// <typeparam name="T">Accepted CLR record type.</typeparam>
        public static TypedImportResult<T> Process<T>(
            IReadOnlyList<ImportDataRow> rows,
            IReadOnlyList<SourceColumnBinding> bindings,
            ImportSchema schema, string sourceId,
            Func<IReadOnlyDictionary<string, string>, T> mapRecord,
            IEnumerable<FieldRangeRule<T>> rangeRules,
            DuplicateIdentifierRule<T> identifierRule,
            IEnumerable<RecordValidationRule<T>> customRules,
            string emptyMessage, string completedMessage, string rejectedMessage,
            bool failFast, string stoppedMessage, CancellationToken cancellationToken,
            string canceledMessage)
        {
            if (string.IsNullOrWhiteSpace(canceledMessage) &&
                cancellationToken.CanBeCanceled)
                throw new ArgumentException(nameof(canceledMessage));
            if (failFast && string.IsNullOrWhiteSpace(stoppedMessage))
                throw new ArgumentException(nameof(stoppedMessage));
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (mapRecord == null) throw new ArgumentNullException(nameof(mapRecord));

            var accepted = new List<T>();
            var diagnostics = new List<ImportDiagnostic>();
            var processedCount = 0;
            var stopped = false;
            var canceled = false;
            var checkDuplicate = identifierRule == null
                ? null : identifierRule.CreateValidator();

            foreach (var row in rows)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    canceled = true;
                    break;
                }
                processedCount++;
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
                    AppendDiagnostics(diagnostics, failFast
                        ? (IEnumerable<ImportDiagnostic>)new[] { requiredErrors[0] } : requiredErrors, row);
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }

                var lengthErrors = schema.ValidateFieldLengths(
                    recordValues, row.Row, row.PhysicalLine);
                if (lengthErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, failFast
                        ? (IEnumerable<ImportDiagnostic>)new[] { lengthErrors[0] } : lengthErrors, row);
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }

                var patternErrors = schema.ValidatePatterns(
                    recordValues, row.Row, row.PhysicalLine);
                if (patternErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, failFast
                        ? (IEnumerable<ImportDiagnostic>)new[] { patternErrors[0] } : patternErrors, row);
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
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
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }
                catch (FormatException)
                {
                    diagnostics.Add(ForRow(row, null, DiagnosticCodes.ConversionFailed));
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }
                catch (OverflowException)
                {
                    diagnostics.Add(ForRow(row, null, DiagnosticCodes.ConversionFailed));
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }

                var rangeErrors = new List<ImportDiagnostic>();
                if (rangeRules != null)
                    foreach (var rule in rangeRules)
                    {
                        var error = rule.Validate(record, row.Row, row.PhysicalLine);
                        if (error != null)
                        {
                            rangeErrors.Add(error);
                            if (failFast) break;
                        }
                    }
                if (rangeErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, failFast
                        ? (IEnumerable<ImportDiagnostic>)new[] { rangeErrors[0] } : rangeErrors, row);
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }

                var customErrors = new List<ImportDiagnostic>();
                if (customRules != null)
                    foreach (var rule in customRules)
                    {
                        var error = rule.Validate(record, row.Row, row.PhysicalLine);
                        if (error != null)
                        {
                            customErrors.Add(error);
                            if (failFast) break;
                        }
                    }
                if (customErrors.Count != 0)
                {
                    AppendDiagnostics(diagnostics, failFast
                        ? (IEnumerable<ImportDiagnostic>)new[] { customErrors[0] } : customErrors, row);
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                    continue;
                }

                var duplicate = checkDuplicate == null
                    ? null : checkDuplicate(record, row.Row, row.PhysicalLine);
                if (duplicate != null)
                {
                    diagnostics.Add(WithProvenance(duplicate, row));
                    if (failFast)
                    {
                        stopped = true;
                        break;
                    }
                }
                else
                    accepted.Add(record);
            }

            canceled = canceled || cancellationToken.IsCancellationRequested;
            var message = canceled ? canceledMessage : stopped ? stoppedMessage
                : processedCount == 0 ? emptyMessage
                : diagnostics.Count == 0 ? completedMessage : rejectedMessage;
            var result = canceled
                ? ImportResult.Canceled(processedCount, diagnostics)
                : stopped ? ImportResult.Stopped(processedCount, diagnostics)
                : ImportResult.Completed(processedCount, diagnostics);
            return new TypedImportResult<T>(sourceId, result, accepted, message);
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
