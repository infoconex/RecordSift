using System;

namespace RecordSift.Application.Ports.Xlsx
{
    /// <summary>Caller-configurable safety budgets for a selected worksheet.</summary>
    public sealed class XlsxReadLimits
    {
        public static readonly int DefaultMaxRows = 100000;
        public static readonly int DefaultMaxColumns = 256;
        public static readonly int DefaultMaxCellCharacters = 32767;

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

        public int MaxRows { get; private set; }
        public int MaxColumns { get; private set; }
        public int MaxCellCharacters { get; private set; }

        public static XlsxReadLimits Default => new XlsxReadLimits(
            DefaultMaxRows, DefaultMaxColumns, DefaultMaxCellCharacters);
    }
}
