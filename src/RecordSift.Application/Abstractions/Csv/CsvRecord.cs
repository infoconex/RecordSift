using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;
using RecordSift.Domain.Results;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Application.Importing.Csv;

namespace RecordSift.Application.Abstractions.Csv
{
    /// <summary>
    /// One raw logical CSV source record before header interpretation. Its ordinal
    /// includes a possible header. PhysicalLine identifies the first source line.
    /// </summary>
    public sealed class CsvRecord
    {
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

        public int SourceRecordNumber { get; private set; }
        public int PhysicalLine { get; private set; }
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
