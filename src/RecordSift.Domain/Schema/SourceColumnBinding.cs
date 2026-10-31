using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Recognized source column; SourceIndex is zero-based, not a logical data-row coordinate.</summary>
    public sealed class SourceColumnBinding
    {
        internal SourceColumnBinding(string name, int sourceIndex)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(DomainMessageResources.BoundColumnName, nameof(name));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            Name = name;
            SourceIndex = sourceIndex;
        }

        /// <summary>Gets the declared source column name.</summary>
        public string Name { get; private set; }
        /// <summary>Gets the zero-based index in the raw source field sequence.</summary>
        public int SourceIndex { get; private set; }
    }
}
