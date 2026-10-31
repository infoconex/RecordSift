using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Validated, immutable source-to-property mapping declarations independent of format.</summary>
    public sealed class PropertyMappingSchema
    {
        /// <summary>Validates a nonempty mapping snapshot against declared source columns and unique destination property names.</summary>
        /// <param name="sourceSchema">Schema defining which source columns are legal.</param>
        /// <param name="mappings">Nonempty source-to-property declarations with unique destinations.</param>
        public PropertyMappingSchema(ImportSchema sourceSchema,
            IEnumerable<SourcePropertyMapping> mappings)
        {
            if (sourceSchema == null) throw new ArgumentNullException(nameof(sourceSchema));
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));

            var allowedSources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in sourceSchema.Columns)
                allowedSources.Add(column.Name);

            var destinations = new HashSet<string>(StringComparer.Ordinal);
            var snapshot = new List<SourcePropertyMapping>();
            foreach (var mapping in mappings)
            {
                if (mapping == null)
                    throw new ArgumentException(DomainMessageResources.PropertyMappingNull,
                        nameof(mappings));
                if (!allowedSources.Contains(mapping.SourceColumnName))
                    throw new ArgumentException(DomainMessageResources.PropertyMappingUndeclaredSource,
                        nameof(mappings));
                if (!destinations.Add(mapping.PropertyName))
                    throw new ArgumentException(DomainMessageResources.PropertyMappingDuplicateDestination,
                        nameof(mappings));
                snapshot.Add(mapping);
            }

            if (snapshot.Count == 0)
                throw new ArgumentException(DomainMessageResources.PropertyMappingEmpty,
                    nameof(mappings));
            SourceSchema = sourceSchema;
            Mappings = new ReadOnlyCollection<SourcePropertyMapping>(snapshot);
        }

        /// <summary>Gets the owning source-column schema.</summary>
        public ImportSchema SourceSchema { get; private set; }
        /// <summary>Gets ordered source-to-property declarations as a read-only snapshot.</summary>
        public IReadOnlyList<SourcePropertyMapping> Mappings { get; private set; }
    }
}
