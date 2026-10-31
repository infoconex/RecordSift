using System;
using NUnit.Framework;
using RecordSift.Application.Importing.Csv;
using RecordSift.Application.Mapping;
using RecordSift.Application.Messages;
using RecordSift.Application.Ports.Csv;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    public sealed class NamespaceOwnershipTests
    {
        [Test]
        public void ApplicationContractsUseDeclaredResponsibilityNamespaces()
        {
            AssertNamespace(typeof(CsvMessageResources), "RecordSift.Application.Messages");
            AssertNamespace(typeof(PrimitivePropertyMapper<System.Collections.Generic.List<string>>),
                "RecordSift.Application.Mapping");
            AssertNamespace(typeof(StringPropertyMapper<System.Collections.Generic.List<string>>),
                "RecordSift.Application.Mapping");
            AssertNamespace(typeof(ICsvRecordReader), "RecordSift.Application.Ports.Csv");
            AssertNamespace(typeof(CsvReadOptions), "RecordSift.Application.Ports.Csv");
            AssertNamespace(typeof(CsvParseResult), "RecordSift.Application.Ports.Csv");
            AssertNamespace(typeof(CsvRecord), "RecordSift.Application.Ports.Csv");
            AssertNamespace(typeof(CsvDataRow), "RecordSift.Application.Importing.Csv");
            AssertNamespace(typeof(CsvHeaderPolicy), "RecordSift.Application.Importing.Csv");
            AssertNamespace(typeof(CsvTableReader), "RecordSift.Application.Importing.Csv");
            AssertNamespace(typeof(CsvTableResult), "RecordSift.Application.Importing.Csv");
        }

        [Test]
        public void ApplicationContractsDoNotUseGenericAbstractionsNamespace()
        {
            foreach (var exportedType in typeof(ICsvRecordReader).Assembly.GetExportedTypes())
                Assert.That(exportedType.Namespace, Does.Not.Contain(".Abstractions."),
                    exportedType.FullName);
        }

        [Test]
        public void ApplicationExportedTypesDoNotUseFlatNamespaces()
        {
            var assembly = typeof(ICsvRecordReader).Assembly;
            foreach (var exportedType in assembly.GetExportedTypes())
                Assert.That(exportedType.Namespace, Does.StartWith("RecordSift.Application."),
                    exportedType.FullName);
        }

        private static void AssertNamespace(Type type, string expected)
        {
            Assert.That(type.Namespace, Is.EqualTo(expected), type.FullName);
        }
    }
}
