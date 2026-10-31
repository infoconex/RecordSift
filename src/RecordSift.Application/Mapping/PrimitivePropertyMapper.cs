using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Mapping
{
    /// <summary>Applies explicit source-to-property mappings using core-owned primitive conversion.</summary>
    public sealed class PrimitivePropertyMapper<T> where T : class, new()
    {
        private readonly PropertyMappingSchema _schema;
        private readonly PrimitiveFieldConverter _converter;
        private readonly IReadOnlyList<PropertyInfo> _properties;
        private readonly IReadOnlyList<bool> _required;

        public PrimitivePropertyMapper(PropertyMappingSchema schema, CultureInfo culture)
            : this(schema, culture, false)
        {
        }

        internal PrimitivePropertyMapper(PropertyMappingSchema schema, CultureInfo culture,
            bool stringOnly)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            _converter = new PrimitiveFieldConverter(culture);

            var requiredSources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in schema.SourceSchema.Columns)
            {
                if (column.IsRequired)
                    requiredSources.Add(column.Name);
            }

            var properties = new List<PropertyInfo>();
            var required = new List<bool>();
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

                if (resolved == null || resolved.GetSetMethod() == null ||
                    resolved.GetIndexParameters().Length != 0 ||
                    !_converter.IsSupported(resolved.PropertyType) ||
                    (stringOnly && resolved.PropertyType != typeof(string)))
                    throw new ArgumentException(
                        stringOnly ? DomainMessageResources.PropertyMappingUnsupportedDestination :
                            DomainMessageResources.PropertyMappingUnsupportedMember, nameof(schema));

                properties.Add(resolved);
                required.Add(requiredSources.Contains(mapping.SourceColumnName));
            }

            _schema = schema;
            _properties = new ReadOnlyCollection<PropertyInfo>(properties);
            _required = new ReadOnlyCollection<bool>(required);
        }

        public T Map(IReadOnlyDictionary<string, string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            // Caller dictionaries can use non-ordinal comparers. Normalize keys so
            // the mapping contract always uses exact-case source identifiers.
            var sourceValues = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var value in values)
                sourceValues.Add(value.Key, value.Value);

            var record = new T();
            for (var i = 0; i < _schema.Mappings.Count; i++)
            {
                var sourceName = _schema.Mappings[i].SourceColumnName;
                string value;
                if (!sourceValues.TryGetValue(sourceName, out value))
                {
                    if (_required[i])
                        throw new ArgumentException(
                            DomainMessageResources.PropertyMappingRequiredSourceMissing, nameof(values));
                    continue;
                }

                try
                {
                    var converted = _converter.Convert(value, _properties[i].PropertyType);
                    _properties[i].SetValue(record, converted, null);
                }
                catch (FormatException)
                {
                    throw new FieldConversionException(sourceName);
                }
                catch (OverflowException)
                {
                    throw new FieldConversionException(sourceName);
                }
                catch (TargetInvocationException)
                {
                    // Public setters may reject converted values. Keep the failure
                    // tied to its source field without exposing setter exception text.
                    throw new FieldConversionException(sourceName);
                }
            }
            return record;
        }
    }
}
