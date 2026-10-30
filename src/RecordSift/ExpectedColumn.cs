using System;

namespace RecordSift
{
    /// <summary>Declared source-column metadata. Presence is not per-record value validation.</summary>
    public sealed class ExpectedColumn
    {
        public ExpectedColumn(string name, bool isRequired)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Expected column name must be nonempty.", nameof(name));
            Name = name;
            IsRequired = isRequired;
        }

        public string Name { get; private set; }
        public bool IsRequired { get; private set; }
    }
}
