using System;
using System.Collections.Generic;
using NUnit.Framework;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Schema;

namespace RecordSift.Domain.Tests
{
    [TestFixture]
    [Category("Unit")]
    public sealed class PositionalBindingTests
    {
        private static ImportSchema Schema()
        {
            return new ImportSchema(new[]
            {
                new ExpectedColumn("Id", true),
                new ExpectedColumn("Name", true),
                new ExpectedColumn("Memo", false)
            });
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-013")]
        [Property("Scenario", "FR-013-SC01")]
        public void FR_013_SC01_NamedAndPositionalBindingsIdentifyEquivalentFields()
        {
            var schema = Schema();
            var named = schema.BindHeaders(new[] { "Memo", "Name", "Id" });
            var positional = schema.BindPositions(3, new[]
            {
                new SourceColumnPosition("Id", 2),
                new SourceColumnPosition("Name", 1),
                new SourceColumnPosition("Memo", 0)
            });

            Assert.That(named.IsSuccessful, Is.True);
            Assert.That(positional.IsSuccessful, Is.True);
            Assert.That(positional.Diagnostics, Is.Empty);
            Assert.That(positional.Bindings, Has.Count.EqualTo(3));
            for (var i = 0; i < named.Bindings.Count; i++)
            {
                Assert.That(positional.Bindings[i].Name, Is.EqualTo(named.Bindings[i].Name));
                Assert.That(positional.Bindings[i].SourceIndex,
                    Is.EqualTo(named.Bindings[i].SourceIndex));
            }
        }

        [Test]
        [Property("Verification", "Acceptance")]
        [Property("Requirement", "FR-013")]
        [Property("Scenario", "FR-013-SC02")]
        public void FR_013_SC02_OutOfRangePositionIsAClassifiedSourceError()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Id", true) });
            var result = schema.BindPositions(2, new[] { new SourceColumnPosition("Id", 2) });

            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Bindings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.InvalidColumnPosition));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(DiagnosticScope.Source));
            Assert.That(result.Diagnostics[0].Row, Is.Null);
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
        }

        [Test]
        public void FirstAndLastZeroBasedPositionsAreValid()
        {
            var schema = new ImportSchema(new[] {
                new ExpectedColumn("First", true), new ExpectedColumn("Last", true)
            });
            var result = schema.BindPositions(3, new[] {
                new SourceColumnPosition("First", 0), new SourceColumnPosition("Last", 2)
            });
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings[0].SourceIndex, Is.Zero);
            Assert.That(result.Bindings[1].SourceIndex, Is.EqualTo(2));
        }

        [Test]
        public void OptionalUnmappedColumnDoesNotProduceAnError()
        {
            var result = Schema().BindPositions(2, new[] {
                new SourceColumnPosition("Id", 0), new SourceColumnPosition("Name", 1)
            });
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void MissingRequiredPositionalDeclarationIsReported()
        {
            var result = Schema().BindPositions(1, new[] { new SourceColumnPosition("Id", 0) });
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Bindings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Name"));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.RequiredColumnMissing));
        }

        [Test]
        public void ConflictingPositionsAreNotSilentlyMapped()
        {
            var result = Schema().BindPositions(2, new[] {
                new SourceColumnPosition("Name", 0), new SourceColumnPosition("Id", 0)
            });
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Bindings, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.AmbiguousColumn));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Name"));
        }

        [Test]
        public void DuplicateMappingDeclarationIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                Schema().BindPositions(3, new[] {
                    new SourceColumnPosition("Id", 0),
                    new SourceColumnPosition("Id", 1)
                }));
        }

        [Test]
        public void UnknownMappingNameIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                Schema().BindPositions(2, new[] { new SourceColumnPosition("Unknown", 0) }));
        }

        [Test]
        public void NoPositionsInAZeroColumnSourceRejectsRequiredFields()
        {
            var result = Schema().BindPositions(0, new SourceColumnPosition[0]);
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("Id"));
            Assert.That(result.Diagnostics[1].Field, Is.EqualTo("Name"));
        }

        [Test]
        public void InvalidConfigurationIsRejectedBeforeBinding()
        {
            Assert.Throws<ArgumentException>(() => new SourceColumnPosition("", 0));
            Assert.Throws<ArgumentException>(() => new SourceColumnPosition(" ", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SourceColumnPosition("Id", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Schema().BindPositions(-1, new SourceColumnPosition[0]));
            Assert.Throws<ArgumentNullException>(() => Schema().BindPositions(1, null));
            Assert.Throws<ArgumentException>(() =>
                Schema().BindPositions(1, new SourceColumnPosition[] { null }));
        }

        [Test]
        public void PositionalMappingSnapshotsInputAndExposesImmutableResults()
        {
            var positions = new List<SourceColumnPosition> {
                new SourceColumnPosition("Id", 0), new SourceColumnPosition("Name", 1)
            };
            var result = Schema().BindPositions(2, positions);
            positions.Clear();
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings, Has.Count.EqualTo(2));
            Assert.That(((IList<SourceColumnBinding>)result.Bindings).IsReadOnly, Is.True);
            Assert.That(typeof(SourceColumnPosition).GetProperty("SourceIndex").GetSetMethod(),
                Is.Null);
        }

        [Test]
        public void PositionsAreIndependentOfMappingDeclarationOrder()
        {
            var result = Schema().BindPositions(2, new[] {
                new SourceColumnPosition("Name", 1), new SourceColumnPosition("Id", 0)
            });
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Bindings[0].Name, Is.EqualTo("Id"));
            Assert.That(result.Bindings[1].Name, Is.EqualTo("Name"));
        }

        [Test]
        public void OutOfRangeOptionalMappingIsStillAnError()
        {
            var schema = new ImportSchema(new[] { new ExpectedColumn("Memo", false) });
            var result = schema.BindPositions(0, new[] { new SourceColumnPosition("Memo", 0) });
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCodes.InvalidColumnPosition));
        }
    }
}
