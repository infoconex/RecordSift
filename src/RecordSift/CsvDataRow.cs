using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift
{
    /// <summary>
    /// Raw data row after explicit header policy. Row is one-based and excludes a
    /// CSV header; SourceRecordNumber includes the header when present.
    /// </summary>
    public sealed class CsvDataRow
    {
        internal CsvDataRow(int row, CsvRecord source)
        {
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            if (source == null) throw new ArgumentNullException(nameof(source));
            Row = row;
            SourceRecordNumber = source.SourceRecordNumber;
            PhysicalLine = source.PhysicalLine;
            Fields = new ReadOnlyCollection<string>(new List<string>(source.Fields));
        }

        public int Row { get; private set; }
        public int SourceRecordNumber { get; private set; }
        public int PhysicalLine { get; private set; }
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
