using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Explicit ordinal association between a declared source column and a CLR property.</summary>
    public sealed class SourcePropertyMapping
    {
        public SourcePropertyMapping(string sourceColumnName, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(sourceColumnName))
                throw new ArgumentException(DomainMessageResources.PropertyMappingSourceName,
                    nameof(sourceColumnName));
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException(DomainMessageResources.PropertyMappingDestinationName,
                    nameof(propertyName));
            SourceColumnName = sourceColumnName;
            PropertyName = propertyName;
        }

        public string SourceColumnName { get; private set; }
        public string PropertyName { get; private set; }
    }
}
