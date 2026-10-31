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
    public sealed class StringPropertyMapper<T> where T : class, new()
    {
        private readonly PrimitivePropertyMapper<T> _mapper;

        public StringPropertyMapper(PropertyMappingSchema schema)
        {
            _mapper = new PrimitivePropertyMapper<T>(schema, CultureInfo.InvariantCulture, true);
        }

        public T Map(IReadOnlyDictionary<string, string> values)
        {
            return _mapper.Map(values);
        }
    }
}
