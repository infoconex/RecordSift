using System.Linq;
using NUnit.Framework;
using RecordSift.Application.Ports.Csv;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class AssemblyIsolationTests
    {
        [Test]
        public void ApplicationTestAssemblyDoesNotReferenceInfrastructureOrCsvHelper()
        {
            var references = typeof(AssemblyIsolationTests).Assembly.GetReferencedAssemblies()
                .Select(assembly => assembly.Name).ToArray();
            Assert.That(references, Does.Contain("RecordSift.Domain"));
            Assert.That(references, Does.Contain("RecordSift.Application"));
            Assert.That(references, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
            Assert.That(references, Does.Not.Contain("CsvHelper"));
        }

        [Test]
        public void ApplicationProductionAssemblyReferencesDomainButNotInfrastructure()
        {
            var references = typeof(ICsvRecordReader).Assembly.GetReferencedAssemblies()
                .Select(assembly => assembly.Name).ToArray();
            Assert.That(typeof(ICsvRecordReader).Assembly.GetName().Name,
                Is.EqualTo("RecordSift.Application"));
            Assert.That(references, Does.Contain("RecordSift.Domain"));
            Assert.That(references, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
            Assert.That(references, Does.Not.Contain("CsvHelper"));
        }
    }
}
