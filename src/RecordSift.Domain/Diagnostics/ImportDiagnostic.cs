using System;

namespace RecordSift.Domain.Diagnostics
{
    /// <summary>
    /// Immutable structured error metadata. Messages are fixed templates selected by
    /// a registered code; never constructed from untrusted source field values.
    /// Field and worksheet identifiers are metadata provided by a caller, not redacted data.
    /// </summary>
    public sealed class ImportDiagnostic
    {
        private ImportDiagnostic(DiagnosticScope scope, int? row, string field,
            string code, string worksheet, int? physicalRow, int? physicalLine)
        {
            if (field != null && string.IsNullOrWhiteSpace(field))
                throw new ArgumentException(DomainMessageResources.DiagnosticFieldBlank, nameof(field));
            if (worksheet != null && string.IsNullOrWhiteSpace(worksheet))
                throw new ArgumentException(DomainMessageResources.DiagnosticWorksheetBlank, nameof(worksheet));
            if (physicalRow.HasValue && physicalRow.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalRow));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));
            if (scope == DiagnosticScope.Record)
            {
                if (!row.HasValue || row.Value < 1)
                    throw new ArgumentOutOfRangeException(nameof(row));
                if (!DiagnosticCodes.IsRecordCode(code))
                    throw new ArgumentException(DomainMessageResources.DiagnosticCodeNotRecord, nameof(code));
            }
            else if (scope == DiagnosticScope.Source)
            {
                if (row.HasValue)
                    throw new ArgumentException(DomainMessageResources.SourceDiagnosticHasRow, nameof(row));
                if (!DiagnosticCodes.IsSourceCode(code))
                    throw new ArgumentException(DomainMessageResources.DiagnosticCodeNotSource, nameof(code));
            }
            else
                throw new ArgumentOutOfRangeException(nameof(scope));

            Scope = scope;
            Row = row;
            Field = field;
            Code = code;
            Message = DiagnosticCodes.GetSafeMessage(code);
            Worksheet = worksheet;
            PhysicalRow = physicalRow;
            PhysicalLine = physicalLine;
        }

        /// <summary>Gets whether this error applies to one data record or to the source as a whole.</summary>
        public DiagnosticScope Scope { get; private set; }
        /// <summary>One-based logical data-record ordinal, excluding headers; null for source errors.</summary>
        public int? Row { get; private set; }
        /// <summary>Trusted source-column metadata when known (name or stable positional identifier).</summary>
        public string Field { get; private set; }
        /// <summary>Gets the stable registered machine-readable diagnostic code.</summary>
        public string Code { get; private set; }
        /// <summary>Gets the invariant, resource-backed safe message for the registered diagnostic code.</summary>
        public string Message { get; private set; }
        /// <summary>Optional worksheet name supplied by a source reader.</summary>
        public string Worksheet { get; private set; }
        /// <summary>Optional one-based physical row (e.g., XLSX row), when reliable.</summary>
        public int? PhysicalRow { get; private set; }
        /// <summary>Optional one-based physical CSV line, only when reliably known.</summary>
        public int? PhysicalLine { get; private set; }

        /// <summary>Creates a record-level diagnostic at a one-based logical row; optional source coordinates are retained when reliable.</summary>
        /// <param name="row">One-based logical data-row ordinal; absent for source-level errors.</param>
        /// <param name="field">Optional trusted source-field metadata; null indicates no field.</param>
        /// <param name="code">Registered, correctly scoped diagnostic code.</param>
        public static ImportDiagnostic ForRecord(int row, string field, string code)
        {
            return ForRecord(row, field, code, null, null, null);
        }

        /// <summary>Creates a record-level diagnostic at a one-based logical row; optional source coordinates are retained when reliable.</summary>
        /// <param name="row">One-based logical data-row ordinal; absent for source-level errors.</param>
        /// <param name="field">Optional trusted source-field metadata; null indicates no field.</param>
        /// <param name="code">Registered, correctly scoped diagnostic code.</param>
        /// <param name="worksheet">Optional worksheet identifier from the source reader.</param>
        /// <param name="physicalRow">Optional one-based XLSX source-row location.</param>
        /// <param name="physicalLine">Optional one-based CSV physical line when reliable.</param>
        public static ImportDiagnostic ForRecord(int row, string field, string code,
            string worksheet, int? physicalRow, int? physicalLine)
        {
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            return new ImportDiagnostic(DiagnosticScope.Record, row, field,
                code, worksheet, physicalRow, physicalLine);
        }

        /// <summary>Creates a source-level diagnostic without a logical row; optional source coordinates are retained when reliable.</summary>
        /// <param name="field">Optional trusted source-field metadata; null indicates no field.</param>
        /// <param name="code">Registered, correctly scoped diagnostic code.</param>
        public static ImportDiagnostic ForSource(string field, string code)
        {
            return ForSource(field, code, null, null, null);
        }

        /// <summary>Creates a source-level diagnostic without a logical row; optional source coordinates are retained when reliable.</summary>
        /// <param name="field">Optional trusted source-field metadata; null indicates no field.</param>
        /// <param name="code">Registered, correctly scoped diagnostic code.</param>
        /// <param name="worksheet">Optional worksheet identifier from the source reader.</param>
        /// <param name="physicalRow">Optional one-based XLSX source-row location.</param>
        /// <param name="physicalLine">Optional one-based CSV physical line when reliable.</param>
        public static ImportDiagnostic ForSource(string field, string code,
            string worksheet, int? physicalRow, int? physicalLine)
        {
            return new ImportDiagnostic(DiagnosticScope.Source, null, field,
                code, worksheet, physicalRow, physicalLine);
        }
    }
}
