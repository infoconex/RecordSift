namespace RecordSift.Application.Importing.Csv
{
    /// <summary>Explicit CSV header policy. No automatic header inference.</summary>
    public enum CsvHeaderPolicy
    {
        WithHeader = 1,
        WithoutHeader = 2
    }
}
