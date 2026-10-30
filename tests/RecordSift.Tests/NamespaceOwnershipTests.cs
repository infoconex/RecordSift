using System;
using NUnit.Framework;
using RecordSift.Application.Abstractions.Csv;
using RecordSift.Application.Importing.Csv;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;
using RecordSift.Domain.Schema;

namespace RecordSift.Tests
{
    [TestFixture]
    public sealed class NamespaceOwnershipTests
    {
        [Test]
        public void DomainContracts_UseDeclaredResponsibilityNamespaces()
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
        public void ApplicationContracts_UseDeclaredResponsibilityNamespaces()
        {
            AssertNamespace(typeof(ICsvRecordReader), "RecordSift.Application.Abstractions.Csv");
            AssertNamespace(typeof(CsvReadOptions), "RecordSift.Application.Abstractions.Csv");
            AssertNamespace(typeof(CsvParseResult), "RecordSift.Application.Abstractions.Csv");
            AssertNamespace(typeof(CsvRecord), "RecordSift.Application.Abstractions.Csv");
            AssertNamespace(typeof(CsvDataRow), "RecordSift.Application.Importing.Csv");
            AssertNamespace(typeof(CsvHeaderPolicy), "RecordSift.Application.Importing.Csv");
            AssertNamespace(typeof(CsvTableReader), "RecordSift.Application.Importing.Csv");
            AssertNamespace(typeof(CsvTableResult), "RecordSift.Application.Importing.Csv");
        }

        [Test]
        public void PublicLayerTypes_DoNotReintroduceFlatRecordSiftNamespace()
        {
            foreach (var assembly in new[] { typeof(ImportSchema).Assembly, typeof(ICsvRecordReader).Assembly })
                foreach (var exportedType in assembly.GetExportedTypes())
                    Assert.That(exportedType.Namespace, Does.StartWith(assembly.GetName().Name + "."),
                        exportedType.FullName + " must be owned by its physical assembly.");
        }

        private static void AssertNamespace(Type type, string expected)
        {
            Assert.That(type.Namespace, Is.EqualTo(expected), type.FullName);
        }
    }
}
