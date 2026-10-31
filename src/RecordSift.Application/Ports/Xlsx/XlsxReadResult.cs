using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>Atomic reader result: ordered rows or a source-scoped diagnostic.</summary>
    public sealed class XlsxReadResult
    {
        private XlsxReadResult(string worksheet, IReadOnlyList<XlsxRecord> records,
            ImportDiagnostic error)
        {
            Worksheet = worksheet;
            Records = records;
            Error = error;
        }

        /// <summary>Gets the selected worksheet name on success, or null on failure.</summary>
        public string Worksheet { get; private set; }
        /// <summary>Gets ordered nonblank source rows in a read-only snapshot; empty on failure.</summary>
        public IReadOnlyList<XlsxRecord> Records { get; private set; }
        /// <summary>Gets the source-scope diagnostic on failure, or null on success.</summary>
        public ImportDiagnostic Error { get; private set; }
        /// <summary>Gets whether the worksheet was read without a source error.</summary>
        public bool IsSuccessful => Error == null;

        /// <summary>Creates a successful worksheet read result by snapshotting the ordered records.</summary>
        /// <param name="worksheet">Selected nonblank worksheet identifier.</param>
        /// <param name="records">Ordered worksheet records, snapshotted in a read-only collection.</param>
        public static XlsxReadResult Success(string worksheet, IEnumerable<XlsxRecord> records)
        {
            if (string.IsNullOrWhiteSpace(worksheet))
                throw new ArgumentException(nameof(worksheet));
            if (records == null) throw new ArgumentNullException(nameof(records));
            return new XlsxReadResult(worksheet,
                new ReadOnlyCollection<XlsxRecord>(new List<XlsxRecord>(records)), null);
        }

        /// <summary>Creates an atomic worksheet read failure with no records or worksheet name.</summary>
        /// <param name="error">One source-scoped diagnostic.</param>
        public static XlsxReadResult Failure(ImportDiagnostic error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (error.Scope != DiagnosticScope.Source)
                throw new ArgumentException(nameof(error));
            return new XlsxReadResult(null,
                new ReadOnlyCollection<XlsxRecord>(new List<XlsxRecord>()), error);
        }
    }
}
