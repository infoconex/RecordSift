using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Explicit binding of a declared schema column to a zero-based source field index.
    /// This is not a logical row ordinal or a destination .NET property mapping.
    /// </summary>
    public sealed class SourceColumnPosition
    {
        public SourceColumnPosition(string name, int sourceIndex)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(DomainMessageResources.SourcePositionName, nameof(name));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            Name = name;
            SourceIndex = sourceIndex;
        }

        public string Name { get; private set; }
        public int SourceIndex { get; private set; }
    }
}
