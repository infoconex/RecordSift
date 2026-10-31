using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecordSift.Application.Importing
{
    /// <summary>Format-neutral logical row, retaining reliable source coordinates.</summary>
    public sealed class ImportDataRow
    {
        /// <summary>Creates a format-neutral one-based logical data row while preserving optional physical-source coordinates; fields are snapshotted.</summary>
        /// <param name="row">One-based logical data-row ordinal excluding source headers.</param>
        /// <param name="fields">Ordered worksheet cell strings, snapshotted on construction.</param>
        /// <param name="worksheet">Selected nonblank worksheet identifier.</param>
        /// <param name="physicalRow">Positive one-based worksheet row location.</param>
        /// <param name="physicalLine">Optional one-based physical CSV line when known.</param>
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

        /// <summary>Gets the one-based logical data row excluding source headers.</summary>
        public int Row { get; private set; }
        /// <summary>Gets the optional source worksheet identifier.</summary>
        public string Worksheet { get; private set; }
        /// <summary>Gets the optional one-based XLSX source row.</summary>
        public int? PhysicalRow { get; private set; }
        /// <summary>Gets the optional one-based physical CSV line.</summary>
        public int? PhysicalLine { get; private set; }
        /// <summary>Gets the source fields in a read-only snapshot.</summary>
        public IReadOnlyList<string> Fields { get; private set; }
    }
}
