using System;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    public sealed class NamespaceOwnershipTests
    {
        [Test]
        public void DomainContractsUseDeclaredResponsibilityNamespaces()
        {
            AssertNamespace(typeof(DiagnosticCodes), "RecordSift.Domain.Diagnostics");
            AssertNamespace(typeof(DiagnosticScope), "RecordSift.Domain.Diagnostics");
            AssertNamespace(typeof(DomainMessageResources), "RecordSift.Domain.Diagnostics");
            AssertNamespace(typeof(ImportDiagnostic), "RecordSift.Domain.Diagnostics");
            AssertNamespace(typeof(ExpectedColumn), "RecordSift.Domain.Schema");
            AssertNamespace(typeof(ImportSchema), "RecordSift.Domain.Schema");
            AssertNamespace(typeof(SchemaBindingResult), "RecordSift.Domain.Schema");
            AssertNamespace(typeof(SourceColumnBinding), "RecordSift.Domain.Schema");
            AssertNamespace(typeof(SourceColumnPosition), "RecordSift.Domain.Schema");
            AssertNamespace(typeof(ImportResult), "RecordSift.Domain.Results");
            AssertNamespace(typeof(ImportStatus), "RecordSift.Domain.Results");
            AssertNamespace(typeof(ImportSummary), "RecordSift.Domain.Results");
        }

        [Test]
        public void DomainExportedTypesDoNotUseFlatNamespaces()
        {
            var assembly = typeof(ImportSchema).Assembly;
            foreach (var exportedType in assembly.GetExportedTypes())
                Assert.That(exportedType.Namespace, Does.StartWith("RecordSift.Domain."),
                    exportedType.FullName);
        }

        private static void AssertNamespace(Type type, string expected)
        {
            Assert.That(type.Namespace, Is.EqualTo(expected), type.FullName);
        }
    }
}
