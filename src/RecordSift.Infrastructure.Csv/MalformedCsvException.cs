using System;

namespace RecordSift.Infrastructure.Csv
{
    internal sealed class MalformedCsvException : Exception
    {
        internal MalformedCsvException(int physicalLine)
        {
            PhysicalLine = physicalLine;
        }

        internal int PhysicalLine { get; private set; }
    }
}
