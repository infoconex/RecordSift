using System;
using System.Globalization;
using System.Text;
using RecordSift.Application.Ports.Reporting;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;

namespace RecordSift.Infrastructure.Reporting
{
    /// <summary>
    /// Presents the existing Domain result without calculating new business state,
    /// accessing source records or exposing caller-supplied result messages.
    /// </summary>
    public sealed class ReadableImportResultFormatterAdapter : IImportResultFormatter
    {
        /// <summary>Formats status, processed/accepted/rejected counts and safe diagnostic metadata using invariant templates, without reading input or including accepted record values.</summary>
        /// <param name="result">Existing in-memory typed result; null throws an argument exception.</param>
        /// <typeparam name="T">Accepted CLR record type; values are not included in output.</typeparam>
        public string Format<T>(TypedImportResult<T> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var hasOneError = result.Diagnostics.Count == 1;
            var header = result.Status == ImportStatus.Canceled
                ? (hasOneError
                    ? ReportingMessageResources.ReadableCanceledSingleErrorSummary
                    : ReportingMessageResources.ReadableCanceledSummary)
                : result.Status == ImportStatus.Stopped
                ? (hasOneError
                    ? ReportingMessageResources.ReadableStoppedSingleErrorSummary
                    : ReportingMessageResources.ReadableStoppedSummary)
                : result.Status == ImportStatus.Failed
                    ? (hasOneError
                        ? ReportingMessageResources.ReadableFailedSingleErrorSummary
                        : ReportingMessageResources.ReadableFailedSummary)
                    : (hasOneError
                        ? ReportingMessageResources.ReadableCompletedSingleErrorSummary
                        : ReportingMessageResources.ReadableCompletedSummary);
            var builder = new StringBuilder(string.Format(CultureInfo.InvariantCulture,
                header,
                result.Summary.ProcessedCount,
                result.Summary.AcceptedCount,
                result.Summary.RejectedCount,
                result.Diagnostics.Count));

            if (result.Status == ImportStatus.Completed &&
                result.Summary.ProcessedCount == 0)
            {
                builder.AppendLine();
                builder.Append(ReportingMessageResources.ReadableNoRecords);
            }

            foreach (var diagnostic in result.Diagnostics)
            {
                builder.AppendLine();
                if (diagnostic.Scope == DiagnosticScope.Record)
                {
                    var template = diagnostic.Field == null
                        ? ReportingMessageResources.ReadableRecordIssue
                        : ReportingMessageResources.ReadableRecordFieldIssue;
                    if (diagnostic.Field == null)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, template,
                            diagnostic.Row.Value, diagnostic.Code, diagnostic.Message);
                    }
                    else
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, template,
                            diagnostic.Row.Value, diagnostic.Field,
                            diagnostic.Code, diagnostic.Message);
                    }
                }
                else
                {
                    var template = diagnostic.Field == null
                        ? ReportingMessageResources.ReadableSourceIssue
                        : ReportingMessageResources.ReadableSourceFieldIssue;
                    if (diagnostic.Field == null)
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, template,
                            diagnostic.Code, diagnostic.Message);
                    }
                    else
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, template,
                            diagnostic.Field, diagnostic.Code, diagnostic.Message);
                    }
                }

                if (diagnostic.Worksheet != null)
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        ReportingMessageResources.ReadableWorksheet, diagnostic.Worksheet);
                if (diagnostic.PhysicalRow.HasValue)
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        ReportingMessageResources.ReadablePhysicalRow,
                        diagnostic.PhysicalRow.Value);
                if (diagnostic.PhysicalLine.HasValue)
                    builder.AppendFormat(CultureInfo.InvariantCulture,
                        ReportingMessageResources.ReadablePhysicalLine,
                        diagnostic.PhysicalLine.Value);
            }

            return builder.ToString();
        }
    }
}
