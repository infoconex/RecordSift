using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Mapping
{
    /// <summary>
    /// Applies explicit format-neutral column bindings to writable string properties.
    /// Conversion of other CLR types is independently specified by FR-011.
    /// </summary>
    public sealed class StringPropertyMapper<T> where T : new()
    {
        private readonly PropertyMappingSchema _schema;
        private readonly IReadOnlyList<PropertyInfo> _properties;

        public StringPropertyMapper(PropertyMappingSchema schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            var properties = new List<PropertyInfo>();
            var candidates = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var mapping in schema.Mappings)
            {
                PropertyInfo resolved = null;
                foreach (var candidate in candidates)
                {
                    if (!string.Equals(candidate.Name, mapping.PropertyName,
                        StringComparison.Ordinal))
                        continue;
                    if (resolved != null)
                        throw new ArgumentException(
                            DomainMessageResources.PropertyMappingUnsupportedDestination,
                            nameof(schema));
                    resolved = candidate;
                }

                if (resolved == null || resolved.PropertyType != typeof(string) ||
                    resolved.GetSetMethod() == null || resolved.GetIndexParameters().Length != 0)
                    throw new ArgumentException(
                        DomainMessageResources.PropertyMappingUnsupportedDestination,
                        nameof(schema));
                properties.Add(resolved);
            }

            _schema = schema;
            _properties = new ReadOnlyCollection<PropertyInfo>(properties);
        }

        public T Map(IReadOnlyDictionary<string, string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var record = new T();
            for (var i = 0; i < _schema.Mappings.Count; i++)
            {
                string value;
                if (values.TryGetValue(_schema.Mappings[i].SourceColumnName, out value))
                    _properties[i].SetValue(record, value, null);
            }
            return record;
        }
    }
}
