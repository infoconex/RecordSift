using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>
    /// Declared source-column metadata. IsRequired controls column presence;
    /// IsValueRequired independently enables per-record blank-value validation.
    /// </summary>
    public sealed class ExpectedColumn
    {
        public ExpectedColumn(string name, bool isRequired)
            : this(name, isRequired, false)
        {
        }

        public ExpectedColumn(string name, bool isRequired, bool isValueRequired)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(DomainMessageResources.ExpectedColumnName, nameof(name));
            Name = name;
            IsRequired = isRequired;
            IsValueRequired = isValueRequired;
        }

        public string Name { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsValueRequired { get; private set; }
    }
}
