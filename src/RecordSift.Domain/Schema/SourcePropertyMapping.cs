using System;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Explicit ordinal association between a declared source column and a CLR property.</summary>
    public sealed class SourcePropertyMapping
    {
        /// <summary>Declares an exact-case association from a source column to a CLR property.</summary>
        /// <param name="sourceColumnName">Declared source column name.</param>
        /// <param name="propertyName">Public writable CLR property name.</param>
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

        /// <summary>Gets the declared source column identifier.</summary>
        public string SourceColumnName { get; private set; }
        /// <summary>Gets the destination CLR property name.</summary>
        public string PropertyName { get; private set; }
    }
}
