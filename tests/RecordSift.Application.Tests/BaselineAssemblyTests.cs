using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using NUnit.Framework;

namespace RecordSift.Application.Tests
{
    [TestFixture]
    public sealed class BaselineAssemblyTests
    {
        private static Assembly LoadProductionAssembly()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "RecordSift.Application.dll");
            Assert.That(File.Exists(path), Is.True, "MSBuild must copy the referenced production assembly.");
            return Assembly.LoadFrom(path);
        }

        [Test]
        public void ProductionAssembly_LoadsWithExpectedIdentity()
        {
            var assembly = LoadProductionAssembly();
            Assert.That(assembly.GetName().Name, Is.EqualTo("RecordSift.Application"));
        }

        [Test]
        public void ProductionAssembly_TargetsDotNetFramework48()
        {
            var assembly = LoadProductionAssembly();
            var attribute = (TargetFrameworkAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(TargetFrameworkAttribute));

            Assert.That(attribute, Is.Not.Null, "Legacy project must emit a framework marker.");
            Assert.That(attribute.FrameworkName, Is.EqualTo(".NETFramework,Version=v4.8"));
        }

        [Test]
        public void ProductionAssembly_UsesExplicitHistoricalVersionMetadata()
        {
            var assembly = LoadProductionAssembly();
            Assert.That(assembly.GetName().Version, Is.EqualTo(new Version(0, 1, 0, 0)));

            var product = (AssemblyProductAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyProductAttribute));
            Assert.That(product, Is.Not.Null);
            Assert.That(product.Product, Is.EqualTo("RecordSift"));
        }
    }
}
