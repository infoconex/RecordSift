using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Application.Ports.Csv;

namespace RecordSift.Application.Importing.Csv
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

        /// <summary>Gets the one-based logical data row, excluding a header.</summary>
        public int Row { get; private set; }
        /// <summary>Gets the one-based raw source-record number, including a header when present.</summary>
        public int SourceRecordNumber { get; private set; }
        /// <summary>Gets the one-based physical CSV line at which the source record starts.</summary>
        public int PhysicalLine { get; private set; }
        /// <summary>Gets the source fields in original order as a read-only snapshot.</summary>
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
