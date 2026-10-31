namespace RecordSift.Domain.Diagnostics
{
    /// <summary>Identifies whether an error refers to a data row or the import source.</summary>
    public enum DiagnosticScope
    {
        /// <summary>An error associated with a specific one-based logical data record.</summary>
        Record,
        /// <summary>An input, schema or source-level error without a logical data row.</summary>
        Source
    }
}
