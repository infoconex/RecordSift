namespace RecordSift.Application.Importing.Csv
{
    /// <summary>Explicit CSV header policy. No automatic header inference.</summary>
    public enum CsvHeaderPolicy
    {
        /// <summary>Treats the first logical CSV record as header names; it does not count as a data row.</summary>
        WithHeader = 1,
        /// <summary>Treats every logical record as data and uses explicit positional schema mapping.</summary>
        WithoutHeader = 2
    }
}
