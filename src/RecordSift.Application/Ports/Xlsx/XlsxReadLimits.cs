using System;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>Caller-configurable safety budgets for a selected worksheet.</summary>
    public sealed class XlsxReadLimits
    {
        private const int DefaultMaxRows = 100000;
        private const int DefaultMaxColumns = 256;
        private const int DefaultMaxCellCharacters = 32767;

        /// <summary>Sets positive per-worksheet row, field-column and per-cell decoded-character budgets.</summary>
        /// <param name="maxRows">Positive maximum physical row index evaluated in a worksheet.</param>
        /// <param name="maxColumns">Positive maximum fields per row in the worksheet.</param>
        /// <param name="maxCellCharacters">Positive maximum decoded UTF-16 characters per worksheet cell.</param>
        public XlsxReadLimits(int maxRows, int maxColumns, int maxCellCharacters)
        {
            if (maxRows < 1) throw new ArgumentOutOfRangeException(nameof(maxRows));
            if (maxColumns < 1) throw new ArgumentOutOfRangeException(nameof(maxColumns));
            if (maxCellCharacters < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCellCharacters));
            MaxRows = maxRows;
            MaxColumns = maxColumns;
            MaxCellCharacters = maxCellCharacters;
        }

        /// <summary>Gets the maximum one-based physical worksheet row index allowed, including a header.</summary>
        public int MaxRows { get; private set; }
        /// <summary>Gets the maximum column count per examined worksheet row.</summary>
        public int MaxColumns { get; private set; }
        /// <summary>Gets the maximum UTF-16 characters permitted in each formatted cell value.</summary>
        public int MaxCellCharacters { get; private set; }

        /// <summary>Gets a new default limit set: 100,000 physical rows, 256 columns per row, and 32,767 UTF-16 characters per cell.</summary>
        public static XlsxReadLimits Default => new XlsxReadLimits(
            DefaultMaxRows, DefaultMaxColumns, DefaultMaxCellCharacters);
    }
}
