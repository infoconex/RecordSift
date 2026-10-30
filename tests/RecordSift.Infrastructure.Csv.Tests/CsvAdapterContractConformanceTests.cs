using NUnit.Framework;
using RecordSift.Infrastructure.Csv;
using RecordSift;
using RecordSift.Tests;

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
