using System;
using System.Linq;
using NUnit.Framework;

namespace RecordSift.Tests
{
    [TestFixture]
    public sealed class DomainTestIsolationTests
    {
        [Test]
        public void DomainUnitTestAssemblyHasNoVendorOrAdapterAssemblyDependency()
        {
            var names = typeof(DomainTestIsolationTests).Assembly
                .GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
            Assert.That(names, Does.Contain("RecordSift"));
            Assert.That(names, Does.Not.Contain("RecordSift.CsvHelperAdapter"));
            Assert.That(names, Does.Not.Contain("CsvHelper"));
        }

        [Test]
        public void DomainAssemblyHasNoThirdPartyParserDependency()
        {
            var names = typeof(ICsvRecordReader).Assembly
                .GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
            Assert.That(names, Does.Not.Contain("CsvHelper"));
            Assert.That(names, Does.Not.Contain("RecordSift.CsvHelperAdapter"));
        }
    }
}
