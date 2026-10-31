using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>Raw stored worksheet values and their one-based physical row.</summary>
    public sealed class XlsxRecord
    {
        /// <summary>Creates an immutable record of one nonblank worksheet row and its physical row index.</summary>
        /// <param name="physicalRow">Positive one-based worksheet row location.</param>
        /// <param name="fields">Ordered worksheet cell strings, snapshotted on construction.</param>
        public XlsxRecord(int physicalRow, IEnumerable<string> fields)
        {
            if (physicalRow < 1) throw new ArgumentOutOfRangeException(nameof(physicalRow));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            PhysicalRow = physicalRow;
            Fields = new ReadOnlyCollection<string>(new List<string>(fields));
        }

        /// <summary>Gets the one-based physical worksheet row number, not a logical data-row ordinal.</summary>
        public int PhysicalRow { get; private set; }
        /// <summary>Gets an ordered read-only snapshot of decoded worksheet cell strings.</summary>
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
