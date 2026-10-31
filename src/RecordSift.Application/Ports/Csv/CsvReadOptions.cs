namespace RecordSift.Application.Ports.Csv
{
    /// <summary>
    /// Vendor-neutral CSV parser configuration for an already-decoded source.
    /// A null options parameter means the default comma delimiter.
    /// </summary>
    public sealed class CsvReadOptions
    {
        private const string DefaultDelimiter = ",";

        /// <summary>Deterministic comma-delimited baseline; no ambient settings.</summary>
        public static CsvReadOptions Default { get; } = new CsvReadOptions(DefaultDelimiter);

        /// <summary>Defines a CSV field delimiter for already-decoded text; invalid delimiters are reported as source diagnostics when read.</summary>
        /// <param name="delimiter">Single CSV separator character; validation is deferred to IsValid.</param>
        public CsvReadOptions(string delimiter)
        {
            Delimiter = delimiter;
        }

        /// <summary>Gets the configured CSV delimiter; must be one allowed character for a valid configuration.</summary>
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
