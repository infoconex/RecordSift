using System;
using NUnit.Framework;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class PropertyMappingSchemaTests
    {
        private static ImportSchema Schema()
        {
            return new ImportSchema(new[] {
                new ExpectedColumn("Customer Id", true),
                new ExpectedColumn("Display Name", true)
            });
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-010")]
        [Property("Scenario", "FR-010-SC02")]
        public void FR_010_SC02_ConflictingSourcesCannotTargetTheSameProperty()
        {
            var exception = Assert.Throws<ArgumentException>(() => new PropertyMappingSchema(
                Schema(), new[] {
                    new SourcePropertyMapping("Customer Id", "CustomerId"),
                    new SourcePropertyMapping("Display Name", "CustomerId")
                }));
            Assert.That(exception.Message, Does.Contain("destination"));
        }

        [Test]
        public void UndeclaredSourceColumnCannotBeMapped()
        {
            Assert.Throws<ArgumentException>(() => new PropertyMappingSchema(
                Schema(), new[] {
                    new SourcePropertyMapping("Unknown Column", "CustomerId")
                }));
        }

        [Test]
        public void MappingMustHaveNonemptyNamesAndPreserveOrdinalCase()
        {
            Assert.Throws<ArgumentException>(() => new SourcePropertyMapping("", "CustomerId"));
            Assert.Throws<ArgumentException>(() => new SourcePropertyMapping("Customer Id", " "));
            Assert.Throws<ArgumentException>(() => new PropertyMappingSchema(
                Schema(), new[] {
                    new SourcePropertyMapping("customer id", "CustomerId")
                }));
        }

        [Test]
        public void EmptyAndNullMappingDeclarationsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new PropertyMappingSchema(
                Schema(), new SourcePropertyMapping[0]));
            Assert.Throws<ArgumentException>(() => new PropertyMappingSchema(
                Schema(), new SourcePropertyMapping[] { null }));
        }

        [Test]
        public void MappingDeclarationsAreImmutableSnapshots()
        {
            var source = new System.Collections.Generic.List<SourcePropertyMapping> {
                new SourcePropertyMapping("Customer Id", "CustomerId")
            };
            var result = new PropertyMappingSchema(Schema(), source);
            source.Clear();
            Assert.That(result.Mappings, Has.Count.EqualTo(1));
            Assert.That(((System.Collections.Generic.IList<SourcePropertyMapping>)result.Mappings)
                .IsReadOnly, Is.True);
        }
    }
}
