using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>Raw stored worksheet values and their one-based physical row.</summary>
    public sealed class XlsxRecord
    {
        public XlsxRecord(int physicalRow, IEnumerable<string> fields)
        {
            if (physicalRow < 1) throw new ArgumentOutOfRangeException(nameof(physicalRow));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            PhysicalRow = physicalRow;
            Fields = new ReadOnlyCollection<string>(new List<string>(fields));
        }

        public int PhysicalRow { get; private set; }
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
