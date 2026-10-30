using System.Linq;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    public sealed class AssemblyIsolationTests
    {
        [Test]
        public void DomainTestAssemblyDoesNotReferenceApplicationOrInfrastructure()
        {
            var references = typeof(AssemblyIsolationTests).Assembly.GetReferencedAssemblies()
                .Select(assembly => assembly.Name).ToArray();
            Assert.That(references, Does.Contain("RecordSift.Domain"));
            Assert.That(references, Does.Not.Contain("RecordSift.Application"));
            Assert.That(references, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
            Assert.That(references, Does.Not.Contain("CsvHelper"));
        }

        [Test]
        public void DomainProductionAssemblyHasNoOutwardDependencies()
        {
            var references = typeof(ImportDiagnostic).Assembly.GetReferencedAssemblies()
                .Select(assembly => assembly.Name).ToArray();
            Assert.That(typeof(ImportDiagnostic).Assembly.GetName().Name,
                Is.EqualTo("RecordSift.Domain"));
            Assert.That(references, Does.Not.Contain("RecordSift.Application"));
            Assert.That(references, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
            Assert.That(references, Does.Not.Contain("CsvHelper"));
        }
    }
}
