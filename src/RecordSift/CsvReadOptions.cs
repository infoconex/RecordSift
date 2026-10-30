using System;

namespace RecordSift
{
    /// <summary>
    /// Vendor-neutral CSV parser configuration for an already-decoded source.
    /// A null options parameter means the default comma delimiter.
    /// </summary>
    public sealed class CsvReadOptions
    {
        public CsvReadOptions(string delimiter)
        {
            Delimiter = delimiter;
        }

        public string Delimiter { get; private set; }

        /// <summary>
        /// The delimiter must be one non-whitespace, non-control character
        /// other than the CSV quotation mark. Tab is explicitly supported.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(Delimiter) || Delimiter.Length != 1)
                    return false;

                char value = Delimiter[0];
                return value == '\t' ||
                       (!char.IsControl(value) && !char.IsWhiteSpace(value) &&
                        !char.IsSurrogate(value) && value != '"');
            }
        }
    }
}
