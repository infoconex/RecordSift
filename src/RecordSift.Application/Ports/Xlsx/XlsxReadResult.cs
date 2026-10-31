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

        public string Worksheet { get; private set; }
        public IReadOnlyList<XlsxRecord> Records { get; private set; }
        public ImportDiagnostic Error { get; private set; }
        public bool IsSuccessful => Error == null;

        public static XlsxReadResult Success(string worksheet, IEnumerable<XlsxRecord> records)
        {
            if (string.IsNullOrWhiteSpace(worksheet))
                throw new ArgumentException(nameof(worksheet));
            if (records == null) throw new ArgumentNullException(nameof(records));
            return new XlsxReadResult(worksheet,
                new ReadOnlyCollection<XlsxRecord>(new List<XlsxRecord>(records)), null);
        }

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
