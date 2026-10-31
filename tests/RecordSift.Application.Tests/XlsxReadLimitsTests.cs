using System;
using System.Linq;
using NUnit.Framework;
using RecordSift.Application.Ports.Xlsx;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class XlsxReadLimitsTests
    {

        [Test]
        public void PublicApplicationLibraryContractsMustNotExposeCompileTimeConstants()
        {
            var literals = typeof(XlsxReadLimits).Assembly.GetExportedTypes()
                .SelectMany(type => type.GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.DeclaredOnly))
                .Where(field => field.IsLiteral && !field.DeclaringType.IsEnum)
                .Select(field => field.DeclaringType.FullName + "." + field.Name)
                .ToArray();

            Assert.That(literals, Is.Empty,
                "Public compile-time constants are embedded into consuming assemblies.");
        }

        [Test]
        public void ConfigurationDefaultFieldsAreNotExposedAsPublicLibraryApi()
        {
            var publicMembers = System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            var csv = typeof(RecordSift.Application.Ports.Csv.CsvReadOptions);
            Assert.That(csv.GetField("DefaultDelimiter", publicMembers), Is.Null);

            var xlsx = typeof(XlsxReadLimits);
            Assert.That(xlsx.GetField("DefaultMaxRows", publicMembers), Is.Null);
            Assert.That(xlsx.GetField("DefaultMaxColumns", publicMembers), Is.Null);
            Assert.That(xlsx.GetField("DefaultMaxCellCharacters", publicMembers), Is.Null);
        }

        [Test]
        public void LibraryConfigurationDefaultsRetainPublishedValues()
        {
            Assert.That(RecordSift.Application.Ports.Csv.CsvReadOptions.Default.Delimiter,
                Is.EqualTo(","));
            Assert.That(XlsxReadLimits.Default.MaxRows, Is.EqualTo(100000));
            Assert.That(XlsxReadLimits.Default.MaxColumns, Is.EqualTo(256));
            Assert.That(XlsxReadLimits.Default.MaxCellCharacters, Is.EqualTo(32767));
        }

        [Test]
        [Property("Requirement", "FR-002")]
        public void ReadLimitsRequirePositiveBudgetsAndExposeConfiguredValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxReadLimits(0, 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxReadLimits(1, 0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new XlsxReadLimits(1, 2, 0));
            var limits = new XlsxReadLimits(10, 5, 20);
            Assert.That(limits.MaxRows, Is.EqualTo(10));
            Assert.That(limits.MaxColumns, Is.EqualTo(5));
            Assert.That(limits.MaxCellCharacters, Is.EqualTo(20));
        }
    }
}
