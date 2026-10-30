# ADR 0007 — Domain-centric ports, adapters and code standards

**Status:** Accepted as engineering direction for issue [#84](https://github.com/infoconex/RecordSift/issues/84). The CsvHelper reader integration is addressed by ADR 0008 / issue #85; broader typed import architecture still evolves.

## Context

The historical net48 baseline initially combined domain-facing source binding with `CsvTableReader` calling a hand-built static `CsvRecordReader`. This couples higher-level import code to a specific parser and makes adapter substitution and isolated unit testing difficult. Further CSV/XLSX support would amplify that risk. RecordSift also needs one clear, compatible C# coding style for human and AI contributions.

## Decision

- Organize the library around domain concepts and application use cases, with inward dependencies and **RecordSift-owned interfaces at external boundaries**.
- Put all third-party CSV/XLSX implementations behind input ports; isolate their package dependencies in adapter assemblies. Only neutral RecordSift types cross these boundaries.
- Supply the adapter through constructor/explicit dependency injection. Keep vendor construction and integration choices outside domain code. Preserve compatibility of existing entry points where feasible.
- Use SOLID principles pragmatically. Introduce interfaces at independently replaceable/testable boundaries, not for all domain models.
- Use the Microsoft .NET/C# coding conventions and Framework Design Guidelines, captured in [C# style](../csharp-style.md) and the root `.editorconfig`. Current C# language version **7.3**, traditional `.csproj`, and .NET Framework **4.8** remain authoritative until a separately approved modernization.
- Use Red → Green → Refactor with unit fakes, real-adapter component tests, API regression tests and observed Windows CI.

## Alternatives

- Keep calling a static home-grown CSV parser: rejected as a permanent architecture because syntax maintenance is unrelated to the RecordSift domain and prevents clean substitution.
- Expose vendor models directly in the public import API: rejected because upgrades could break clients and domain rules.
- Add a DI container or many speculative abstractions: rejected as unnecessary for a reusable library.
- Immediately split every domain concern into its own project: deferred; create physical adapter assemblies when a third-party dependency is introduced, protecting the core assembly without churn.

## Verifying substitutability

The core owns the complete `ICsvRecordReader` behavioral contract (neutral types, header independence, provenance, bounded reads, safe diagnostics, caller ownership, deterministic failures). **Issue #91** physically separates core NUnit tests from adapter NUnit tests and defines a reusable vendor-neutral NUnit contract fixture. Adapter test assemblies derive from the fixture and must satisfy the same behavior with their real parser. The core test assembly must build/run with no vendor DLL; Windows CI checks the direction.

## Migration and evidence

[#85](https://github.com/infoconex/RecordSift/issues/85) owns the reader-port migration, parser selection, ADR 0005 supersession via ADR 0008 and characterization tests. It must preserve or explicitly document behavior. FR-001 #2 and FR-004 #14 remain independently traceable; this **architecture ADR does not establish implementation or passing tests**.
