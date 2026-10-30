using NUnit.Framework;
using RecordSift;
using RecordSift.Infrastructure.Csv;
using RecordSift.Tests;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;
using RecordSift.Domain.Results;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Application.Importing.Csv;

namespace RecordSift.Infrastructure.Csv.Tests
{
    /// <summary>
    /// Real CSV infrastructure adapter must meet the core-owned, vendor-independent
    /// CsvRecordReaderContractTests. Future adapters derive from the same suite.
    /// </summary>
    [TestFixture]
    public sealed class CsvAdapterContractConformanceTests : CsvRecordReaderContractTests
    {
        protected override ICsvRecordReader CreateReader()
        {
            return new CsvRecordReaderAdapter();
        }
    }
}
