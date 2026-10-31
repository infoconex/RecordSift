using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift.Application.Importing
{
    /// <summary>Format-neutral logical row, retaining reliable source coordinates.</summary>
    public sealed class ImportDataRow
    {
        public ImportDataRow(int row, IEnumerable<string> fields, string worksheet = null,
            int? physicalRow = null, int? physicalLine = null)
        {
            if (row < 1) throw new ArgumentOutOfRangeException(nameof(row));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            if (physicalRow.HasValue && physicalRow.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalRow));
            if (physicalLine.HasValue && physicalLine.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(physicalLine));
            Row = row;
            Worksheet = worksheet;
            PhysicalRow = physicalRow;
            PhysicalLine = physicalLine;
            Fields = new ReadOnlyCollection<string>(new List<string>(fields));
        }

        public int Row { get; private set; }
        public string Worksheet { get; private set; }
        public int? PhysicalRow { get; private set; }
        public int? PhysicalLine { get; private set; }
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
