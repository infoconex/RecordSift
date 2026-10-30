# ADR 0018 — Separate Domain and Application unit-test assemblies

**Status:** Accepted through [engineering issue #105](https://github.com/infoconex/RecordSift/issues/105). Follows [ADR 0013](0013-physical-domain-application-infrastructure-assemblies.md) and [ADR 0017](0017-application-ports-namespace.md); strengthens the core/adapter test separation from [ADR 0011](0011-independent-core-contract-tests.md).

## Context

The previous `RecordSift.Tests` assembly referenced **both** Domain and Application, while `RecordSift.Infrastructure.Csv.Tests` referenced the combined test assembly to reuse an abstract CSV reader conformance fixture. This proves vendor independence but does not enforce Domain-only unit tests at a physical assembly boundary. The architecture's dependency rule should apply to tests as far as practical.

## Decision

Use three separately compiled and executed traditional .NET Framework 4.8 NUnit assemblies:

| Test assembly | Approved project references | Responsibility |
| --- | --- | --- |
| `RecordSift.Domain.Tests` | `RecordSift.Domain` | Domain result/schema/diagnostic/message invariants. No Application dependency. |
| `RecordSift.Application.Tests` | `RecordSift.Application`, `RecordSift.Domain` | Application use case/port policy, in-memory fake tests, shared abstract `CsvRecordReaderContractTests`. |
| `RecordSift.Infrastructure.Csv.Tests` | `RecordSift.Application.Tests`, `RecordSift.Application`, `RecordSift.Domain`, `RecordSift.Infrastructure.Csv` | Real CsvHelper adapter integration and inherited conformance tests. |

NUnit is an approved development-only test runner, not a third-party import implementation. The NUnit console runner is restored from the Domain test project's pinned `packages.config`; other test projects pin only the NUnit framework.

The old combined project and namespace are removed. Move test fixtures to layer-identifying namespaces; split namespace and assembly isolation assertions without weakening any test. Move the CSV default delimiter test to Application because `CsvReadOptions` belongs to the input port, not Domain.

## Enforcement

Update traditional solution project identities, project references and `Compile` inclusions, plus the architecture script, with explicit negative fixtures rejecting Domain tests -> Application, Application tests -> Infrastructure, and adapter tests -> Domain tests. Verify exact declared C# namespaces and source ownership. The Windows pipeline must execute three independent NUnit suites, report their individual counts, preserve the previously verified total of at least **126** tests, check that Domain test output does not contain Application/adapter/vendor binaries, and keep adapter conformance inherited from Application tests.

This changes test assembly and fixture full names (including scenario traceability references), not production contracts. Historical CI run evidence remains historical and no full typed-import acceptance is implied.

## Alternatives rejected

A single `Core.Tests` assembly retains an unnecessary outward Domain test dependency. Copying the conformance fixture into each adapter project risks divergent observable contracts. Moving a CSV `TextReader` port fixture into Domain would make business-layer test ownership misleading. Additional test-support assemblies are not needed at current scale.
