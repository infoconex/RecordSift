using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift.Application.Ports.Csv
{
    /// <summary>
    /// One raw logical CSV source record before header interpretation. Its ordinal
    /// includes a possible header. PhysicalLine identifies the first source line.
    /// </summary>
    public sealed class CsvRecord
    {
        /// <summary>Creates an immutable snapshot of one raw CSV source record and its original coordinates.</summary>
        /// <param name="sourceRecordNumber">Positive source-record ordinal including any header.</param>
        /// <param name="physicalLine">Positive one-based initial physical line of the logical CSV record.</param>
        /// <param name="fields">Original ordered field strings; snapshotted into a read-only list.</param>
        public CsvRecord(int sourceRecordNumber, int physicalLine, IEnumerable<string> fields)
        {
            if (sourceRecordNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(sourceRecordNumber));
            if (physicalLine < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));
            SourceRecordNumber = sourceRecordNumber;
            PhysicalLine = physicalLine;
            Fields = new ReadOnlyCollection<string>(new List<string>(fields));
        }

        /// <summary>Gets the one-based record ordinal including a source header if present.</summary>
        public int SourceRecordNumber { get; private set; }
        /// <summary>Gets the one-based starting physical line of this logical record, including multiline CSV fields.</summary>
        public int PhysicalLine { get; private set; }
        /// <summary>Gets the original raw CSV fields as a read-only snapshot.</summary>
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
