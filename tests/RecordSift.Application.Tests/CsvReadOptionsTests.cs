using NUnit.Framework;
using RecordSift.Application.Ports.Csv;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    public sealed class CsvReadOptionsTests
    {
        [Test]
        public void CsvDefaultDelimiterIsOwnedByApplicationPortConfiguration()
        {
            Assert.That(CsvReadOptions.Default.Delimiter, Is.EqualTo(","));
            Assert.That(CsvReadOptions.Default.IsValid, Is.True);
        }
    }
}
