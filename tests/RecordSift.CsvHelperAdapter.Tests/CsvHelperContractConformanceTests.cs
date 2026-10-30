using NUnit.Framework;
using RecordSift.Adapters.CsvHelper;

namespace RecordSift.Tests
{
    /// <summary>
    /// Real CsvHelper adapter must meet the core-owned, vendor-independent
    /// CsvRecordReaderContractTests. Future adapters derive from the same suite.
    /// </summary>
    [TestFixture]
    public sealed class CsvHelperContractConformanceTests : CsvRecordReaderContractTests
    {
        protected override ICsvRecordReader CreateReader()
        {
            return new CsvHelperRecordReader();
        }
    }
}
