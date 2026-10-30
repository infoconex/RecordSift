using System;

namespace RecordSift
{
    /// <summary>Recognized source column; SourceIndex is zero-based, not a logical data-row coordinate.</summary>
    public sealed class SourceColumnBinding
    {
        internal SourceColumnBinding(string name, int sourceIndex)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bound column name must be nonempty.", nameof(name));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            Name = name;
            SourceIndex = sourceIndex;
        }

        public string Name { get; private set; }
        public int SourceIndex { get; private set; }
    }
}
