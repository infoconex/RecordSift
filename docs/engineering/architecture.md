# Domain-centric architecture and dependency boundaries

**Policy:** Engineering issue [#84](https://github.com/infoconex/RecordSift/issues/84). Applies to all new implementation work, including AI-assisted changes. See [ADR 0007](decisions/0007-domain-ports-and-adapters.md).

RecordSift is a reusable import **library**, not an ASP.NET application. Use domain-centric, ports-and-adapters design with pragmatic separation of responsibilities; do not add a web framework, DI container, or elaborate DDD patterns without demonstrated need.

## Ownership and dependency direction

```text
Consuming application / composition root
                |
        RecordSift application orchestration
        (import sequencing, source policy, results)
                |
   +------------+-------------------------+
   |                                      |
RecordSift domain                 RecordSift-owned input ports
(schema, bindings,                (contracts + neutral records,
 validation, diagnostics,         provenance, bounded options)
 result invariants)                         ^
                                            |
                       infrastructure adapters: CSV / XLSX
                       (only here: parser vendor packages)
```

- **Domain:** owns import vocabulary and invariants: required columns, schema bindings, converted/validated records, diagnostics, statuses and counting. Do not introduce `CsvHelper`, XLSX library classes, filesystem paths, `TextReader`, or vendor exceptions into domain policies/entities. Prefer immutable state and explicit configuration.
- **Application:** coordinates source reading, header and binding policies, conversion, validation, failure translation and result construction. Application services depend on ports defined by RecordSift. `TextReader` / `Stream` from the base class library may appear at an application boundary where appropriate, but not in domain entities.
- **Infrastructure adapters:** depend inward on the RecordSift-owned contracts; encapsulate parsing, physical IO, and library-specific configuration. An adapter converts third-party records/errors into our records, provenance and safe diagnostic codes. A vendor package must not be needed to compile the domain/application-core assembly.
- **Composition:** callers select and inject an adapter through a constructor or explicit dependency parameter. Do not hide vendor construction inside a domain service, use global mutable registries, or require a DI framework. An optional convenience facade can preserve an existing public API while delegating to injected implementation.

The [#95](https://github.com/infoconex/RecordSift/issues/95) architecture correction and [#97](https://github.com/infoconex/RecordSift/issues/97) infrastructure naming follow-up makes all three production layers **separate .NET Framework 4.8 CLR assemblies**: `RecordSift.Domain`, `RecordSift.Application`, and `RecordSift.Infrastructure.Csv`. The original combined `RecordSift.dll` is retired (binary-breaking); domain and application public namespaces now follow their owning assemblies and responsibilities ([ADR 0015](decisions/0015-layer-aligned-public-namespaces.md)); these source/CLR type-name changes are pre-release breaking corrections. The adapter references Application and Domain, but the dependency direction cannot be reversed. Domain/core source has no third-party parser reference. These are still evolving boundaries, not a claim that the full importer is implemented. Project splitting is intentional here: it gives the architecture compiler-enforced boundaries, not merely folders and guidelines.

## Physical solution structure and enforced graph

```text
src/
  RecordSift.Domain/                        (no ProjectReference)
    Diagnostics/                            diagnostic IDs, safe messages/resources
    Schema/                                 declarations, bindings and schema invariants
    Results/                                import outcome and aggregate invariants
  RecordSift.Application/                   -> RecordSift.Domain
    Ports/Csv/                       owned ICsvRecordReader and neutral DTOs/options
    Importing/Csv/                          table orchestration and header policy
  RecordSift.Infrastructure.Csv/            -> Application + Domain
    CsvRecordReaderAdapter.cs              real CSV adapter backed by CsvHelper
tests/
  RecordSift.Tests/                         -> Domain + Application; no parser vendor
  RecordSift.Infrastructure.Csv.Tests/        -> Domain + Application + real adapter
```

The **Domain assembly does not reference `System.IO`, `TextReader`, the application assembly, or any third-party parser**. Application owns the reader port and may use framework IO types at an input boundary, but never a vendor parser type. Infrastructure translates parser exceptions into Domain-owned codes and resource-backed messages.

`scripts/validate_architecture_layers.py` verifies source ownership, explicit traditional project `Compile` items, resource identity, legal `ProjectReference` edges, test assembly isolation and negative dependency test cases in CI. The validator also enforces exact responsibility-aligned namespaces and rejects flat declarations; independent NUnit reflection checks verify the public API namespace surface. The compiler enforces the same references. See [ADR 0013](decisions/0013-physical-domain-application-infrastructure-assemblies.md).

The concrete `RecordSift.Infrastructure.Csv.CsvRecordReaderAdapter : RecordSift.Application.Ports.Csv.ICsvRecordReader` translates the third-party CsvHelper parser into our Application-owned contract. We reserve the suffix `Adapter` for such concrete implementations; it is **not** part of the application port's name. The project is named after the source format rather than the vendor package, so a future parser swap does not change our architectural vocabulary. The Application's CSV input port and neutral contract types live in `Ports/Csv`, as decided in [ADR 0017](decisions/0017-application-ports-namespace.md). See [ADR 0014](decisions/0014-infrastructure-csv-adapter-naming.md).

## Replaceable source-reader contracts

The CSV parser migration is tracked by [#85](https://github.com/infoconex/RecordSift/issues/85). Use a RecordSift-owned narrow port (`ICsvRecordReader`, introduced test-first) that produces existing RecordSift-owned records and diagnostics. A CSV adapter implements the port. A future XLSX reader can have its **own** source-specific port; do not force a `TextReader` contract onto binary Excel input. A format-neutral source record abstraction is worthwhile only when both adapters need it.

The contract must explicitly define: ownership/disposal of caller streams/readers; explicit culture and encoding boundaries; column order and source-record provenance; header semantics; bounded reading; failure atomicity; safe diagnostic translation; deterministic outputs. Do not expose third-party data models, configuration objects, exceptions, loggers, or callback types in public RecordSift contracts.

The application orchestration can be unit-tested with a hand-written fake port returning known records, schema mismatches, and failures. Adapter component tests must exercise the **real parser** and malformed-input behavior. Public integration tests must exercise the full import route. Fake tests alone do not verify quoted CSV syntax, line provenance, memory budgets, or license security.

## SOLID review guide

| Principle | RecordSift expectation |
| --- | --- |
| Single responsibility | Keep tokenization, decoding, header policy, schema binding, validation, and result reporting separate; change a module for one kind of reason. |
| Open/closed | Support additional source adapters and validators without changing established domain rules or rewriting import orchestration. |
| Liskov substitution | Every adapter and fake honors the same contract for order, failure semantics, limits and ownership; swapping must not silently weaken guarantees. |
| Interface segregation | Small consumer-driven input/validation contracts, not a universal importer interface with methods irrelevant to some source types. |
| Dependency inversion | Import use cases depend on owned abstractions, never on parser package types or concrete vendor constructors. |

Apply these principles to **real seams**. Do not create interfaces for every DTO/value object, build a service locator, or introduce speculative extensibility.

## Domain contract and test ownership

RecordSift core owns **all observable import behavior**: neutral input/output records, provenance, stable diagnostic codes/scopes and resource-backed messages, default options, statuses and safe limits. Infrastructure adapters may use vendor exceptions internally but must return those exact core contracts. Domain unit tests use fakes and core types only; real-adapter conformance tests verify the same observable output. The issue #91 change splits core and real-adapter NUnit assemblies and runs a core-owned behavioral contract fixture against the real CsvHelper adapter. The core test DLL has no parser dependency.

Core message resources and defaults are defined by [ADR 0010](decisions/0010-domain-message-resources.md), and [ADR 0016](decisions/0016-typed-domain-resource-messages.md) establishes named public message accessors. The raw resource-key lookup is internal to Domain; Application and Infrastructure must never refer to embedded `Argument_*` key strings. Diagnostic code values remain distinct machine-readable constants.

## Enforcement and review

1. A third-party integration needs a recorded compatibility/licensing/security review and pinned versions. Constrain package references to adapter/test projects. Do not replace a proven parser with custom grammar logic without an explicit ADR and tests.
2. Prefer constructor injection and hand-written fakes for small interfaces; use mocking libraries only when they add clear value. Unit-test policies without IO and component-test adapters with real input.
3. Preserve public API and JSON compatibility with characterization/contract tests and an ADR for intentional change. In particular keep source/row diagnostics safe, deterministic, and explicit about physical versus logical rows.
4. Review namespace and assembly references when adding dependencies; CI runs an automated architecture/dependency gate over `.csproj` references and folders. Microsoft naming rules are independently enforced with Roslyn IDE1006 and the constant naming guard.
5. Use [Microsoft coding standards](csharp-style.md), [TDD strategy](../testing/strategy.md), and the [PR checklist](../../.github/pull_request_template.md). Exceptions belong in a scoped ADR or issue, never as an undocumented shortcut.
