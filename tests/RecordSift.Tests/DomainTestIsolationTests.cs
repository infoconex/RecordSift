using System;
using System.Linq;
using NUnit.Framework;
using RecordSift.Application.Ports.Csv;
using RecordSift.Domain.Diagnostics;

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
            Assert.That(names, Does.Contain("RecordSift.Application"));
            Assert.That(names, Does.Contain("RecordSift.Domain"));
            Assert.That(names, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
            Assert.That(names, Does.Not.Contain("CsvHelper"));
        }

        [Test]
        public void ApplicationReferencesDomainButNotInfrastructure()
        {
            var names = typeof(ICsvRecordReader).Assembly.GetReferencedAssemblies()
                .Select(assembly => assembly.Name).ToArray();
            Assert.That(typeof(ICsvRecordReader).Assembly.GetName().Name,
                Is.EqualTo("RecordSift.Application"));
            Assert.That(names, Does.Contain("RecordSift.Domain"));
            Assert.That(names, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
            Assert.That(names, Does.Not.Contain("CsvHelper"));
        }

        [Test]
        public void DomainAssemblyHasNoThirdPartyParserDependency()
        {
            var names = typeof(ImportDiagnostic).Assembly
                .GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
            Assert.That(typeof(ImportDiagnostic).Assembly.GetName().Name, Is.EqualTo("RecordSift.Domain"));
            Assert.That(names, Does.Not.Contain("RecordSift.Application"));
            Assert.That(names, Does.Not.Contain("CsvHelper"));
            Assert.That(names, Does.Not.Contain("RecordSift.Infrastructure.Csv"));
        }
    }
}
