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
    /// <typeparam name="T">CLR record type constructed or processed by this service.</typeparam>
    public sealed class PrimitivePropertyMapper<T> where T : class, new()
    {
        private readonly PropertyMappingSchema _schema;
        private readonly PrimitiveFieldConverter _converter;
        private readonly CultureInfo _culture;
        private readonly IReadOnlyDictionary<string, FieldConversionRule> _customConverters;
        private readonly IReadOnlyList<PropertyInfo> _properties;
        private readonly IReadOnlyList<bool> _required;

        /// <summary>Validates declared destination properties and constructs a mapper with a defensive snapshot of the explicit conversion culture.</summary>
        /// <param name="schema">Declared source field schema used for header matching.</param>
        /// <param name="culture">Explicit parsing culture, defensively snapshotted.</param>
        public PrimitivePropertyMapper(PropertyMappingSchema schema, CultureInfo culture)
            : this(schema, culture, false, null)
        {
        }

        /// <summary>Creates an explicit-culture mapper with opt-in, format-neutral per-field converters. Rules are snapshotted and validated before import.</summary>
        /// <param name="schema">Declared canonical source-to-destination mappings.</param>
        /// <param name="culture">Explicit conversion culture, copied into a read-only snapshot.</param>
        /// <param name="customConverters">Optional independent rules keyed by exact-case canonical source field; duplicates, unknown fields and null rules are invalid.</param>
        public PrimitivePropertyMapper(PropertyMappingSchema schema, CultureInfo culture,
            IEnumerable<FieldConversionRule> customConverters)
            : this(schema, culture, false, customConverters)
        {
        }

        internal PrimitivePropertyMapper(PropertyMappingSchema schema, CultureInfo culture,
            bool stringOnly)
            : this(schema, culture, stringOnly, null)
        {
        }

        private PrimitivePropertyMapper(PropertyMappingSchema schema, CultureInfo culture,
            bool stringOnly, IEnumerable<FieldConversionRule> customConverters)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            _converter = new PrimitiveFieldConverter(culture);
            _culture = CultureInfo.ReadOnly((CultureInfo)culture.Clone());

            var mappedSources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in schema.Mappings)
                mappedSources.Add(mapping.SourceColumnName);
            var registered = new Dictionary<string, FieldConversionRule>(StringComparer.Ordinal);
            if (customConverters != null)
                foreach (var rule in customConverters)
                {
                    if (rule == null)
                        throw new ArgumentException(
                            DomainMessageResources.ConversionRuleNull, nameof(customConverters));
                    if (!mappedSources.Contains(rule.Field))
                        throw new ArgumentException(
                            DomainMessageResources.ConversionRuleUndeclared, nameof(customConverters));
                    if (registered.ContainsKey(rule.Field))
                        throw new ArgumentException(
                            DomainMessageResources.ConversionRuleDuplicate, nameof(customConverters));
                    registered.Add(rule.Field, rule);
                }
            _customConverters = new ReadOnlyDictionary<string, FieldConversionRule>(registered);

            var requiredSources = new HashSet<string>(StringComparer.Ordinal);
            var configuredDefaults = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var column in schema.SourceSchema.Columns)
            {
                if (column.IsRequired)
                    requiredSources.Add(column.Name);
                else if (column.DefaultValue != null)
                    configuredDefaults.Add(column.Name, column.DefaultValue);
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
                    (!_converter.IsSupported(resolved.PropertyType) &&
                        !_customConverters.ContainsKey(mapping.SourceColumnName)) ||
                    (stringOnly && resolved.PropertyType != typeof(string)))
                    throw new ArgumentException(
                        stringOnly ? DomainMessageResources.PropertyMappingUnsupportedDestination :
                            DomainMessageResources.PropertyMappingUnsupportedMember, nameof(schema));

                string rawDefault;
                if (configuredDefaults.TryGetValue(mapping.SourceColumnName, out rawDefault))
                {
                    // Validate against the actual destination type and configured culture
                    // before any input rows are accepted; never expose the default value.
                    try
                    {
                        FieldConversionRule customRule;
                        if (_customConverters.TryGetValue(mapping.SourceColumnName, out customRule))
                            ConvertCustom(customRule, rawDefault, resolved.PropertyType);
                        else
                            _converter.Convert(rawDefault, resolved.PropertyType);
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException(
                            DomainMessageResources.ConversionInvalidValue, nameof(schema));
                    }
                    catch (OverflowException)
                    {
                        throw new ArgumentException(
                            DomainMessageResources.ConversionInvalidValue, nameof(schema));
                    }
                }

                properties.Add(resolved);
                required.Add(requiredSources.Contains(mapping.SourceColumnName));
            }

            _schema = schema;
            _properties = new ReadOnlyCollection<PropertyInfo>(properties);
            _required = new ReadOnlyCollection<bool>(required);
        }

        /// <summary>Maps source strings keyed by exact-case schema names into a new CLR record; conversion errors identify the field without preserving sensitive raw values.</summary>
        /// <param name="values">Source strings keyed by exact-case declared column names.</param>
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

                object converted;
                try
                {
                    FieldConversionRule customRule;
                    converted = _customConverters.TryGetValue(sourceName, out customRule)
                        ? ConvertCustom(customRule, value, _properties[i].PropertyType)
                        : _converter.Convert(value, _properties[i].PropertyType);
                }
                catch (FormatException)
                {
                    throw new FieldConversionException(sourceName);
                }
                catch (OverflowException)
                {
                    throw new FieldConversionException(sourceName);
                }

                try
                {
                    _properties[i].SetValue(record, converted, null);
                }
                catch (TargetInvocationException)
                {
                    // Preserve the established classification for a destination setter
                    // rejecting the assigned value, not converter programming failures.
                    throw new FieldConversionException(sourceName);
                }
            }
            return record;
        }

        private object ConvertCustom(FieldConversionRule rule, string value, Type destinationType)
        {
            object converted;
            try
            {
                converted = rule.Convert(value, _culture);
            }
            catch (FormatException)
            {
                // Expected data rejections are record-specific; never retain untrusted messages.
                throw new FieldConversionException(rule.Field);
            }
            catch (OverflowException)
            {
                throw new FieldConversionException(rule.Field);
            }

            var target = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            if ((converted == null && destinationType.IsValueType &&
                    Nullable.GetUnderlyingType(destinationType) == null) ||
                (converted != null && !target.IsInstanceOfType(converted)))
                throw new InvalidOperationException(DomainMessageResources.ConversionUnsupportedType);
            return converted;
        }
    }
}
