using System;
using System.Collections.Generic;
using System.Globalization;
using RecordSift.Domain.Schema;

namespace RecordSift.Application.Mapping
{
    /// <summary>
    /// Preserves the initial string-only property mapping contract; nonstring
    /// conversion is explicitly enabled through PrimitivePropertyMapper instead.
    /// </summary>
    /// <typeparam name="T">CLR record type constructed or processed by this service.</typeparam>
    public sealed class StringPropertyMapper<T> where T : class, new()
    {
        private readonly PrimitivePropertyMapper<T> _mapper;

        /// <summary>Constructs a string-only property mapper using invariant culture; destination members must be publicly writable strings.</summary>
        /// <param name="schema">Declared source field schema used for header matching.</param>
        public StringPropertyMapper(PropertyMappingSchema schema)
        {
            _mapper = new PrimitivePropertyMapper<T>(schema, CultureInfo.InvariantCulture, true);
        }

        /// <summary>Maps source values to a new record using the declared string-only source-to-property mappings.</summary>
        /// <param name="values">Source strings keyed by exact-case declared column names.</param>
        public T Map(IReadOnlyDictionary<string, string> values)
        {
            return _mapper.Map(values);
        }
    }
}
