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
        /// <summary>Associates a declared column with a nonnegative, zero-based source field index.</summary>
        /// <param name="name">Exact-case source field name.</param>
        /// <param name="sourceIndex">Nonnegative zero-based index of the source field.</param>
        public SourceColumnPosition(string name, int sourceIndex)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(DomainMessageResources.SourcePositionName, nameof(name));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            Name = name;
            SourceIndex = sourceIndex;
        }

        /// <summary>Gets the schema column name being positioned.</summary>
        public string Name { get; private set; }
        /// <summary>Gets the zero-based source field index (not a row number).</summary>
        public int SourceIndex { get; private set; }
    }
}
