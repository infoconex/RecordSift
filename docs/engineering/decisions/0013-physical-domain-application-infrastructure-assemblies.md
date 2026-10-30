# ADR 0013 — Independent Domain, Application and Infrastructure assemblies

**Status:** Accepted through [engineering issue #95](https://github.com/infoconex/RecordSift/issues/95). This supersedes the temporary single-core-project approach in ADR 0007.

## Context

A single `RecordSift.dll` with domain/app files in one project, even in separate folders, cannot enforce inward architecture at compile time. The user approved a one-time **breaking baseline correction**, prioritizing correct design before implementing additional requirements.

## Decision

Split source into distinct traditional .NET Framework 4.8 / C# 7.3 assemblies:

- **`RecordSift.Domain`**: diagnostics and invariant resource-backed messages, import schema and column binding, immutable import result/status/summary. References only the .NET base class library; no reader, filesystem, CSV parser or application types.
- **`RecordSift.Application`**: application-level `ICsvRecordReader` input port, neutral source records/options, CSV header/row coordination. Depends **only** on Domain and the base class library; never on a concrete infrastructure adapter.
- **`RecordSift.CsvHelperAdapter`**: actual third-party parser integration in `Infrastructure/CsvHelper`, depends inward on Application/Domain and pinned CsvHelper packages. No parser types cross the RecordSift-owned interface.

Two NUnit assemblies remain independent: `RecordSift.Tests` references only Application, Domain, NUnit and uses fakes; `RecordSift.CsvHelperAdapter.Tests` invokes the actual library against the same vendor-neutral contract fixture. CI runs both suites individually.

The original combined `RecordSift.dll` is **removed**, so this is a binary-breaking architectural correction, approved before shipping a typed import API. The public source namespace `RecordSift` is retained to avoid unrelated bulk renaming; the CLR project/assembly identity controls the physical layer. Any future namespace reorganization can be deliberate and separately tested.

Domain owns all error-code and text contracts. `DomainMessageResources.Get` is public to enable resource access from Application without copying string messages. The resource manifest name `RecordSift.Resources.DomainMessages.resources` remains unchanged. Domain schema and result invariants cannot depend on parser or IO types.

## Enforcement

- Compile-time project references: Domain has none; Application references Domain; the vendor assembly references Application and Domain.
- `scripts/validate_architecture_layers.py` asserts project graph, folder ownership, no unexpected loose files, model/resource ownership and explicit `Compile` lists; its negative self-tests inject forbidden dependency edges.
- Windows CI verifies the architecture, traditional MSBuild, Roslyn IDE1006, both NUnit suites and security/package audit; documentation CI checks resource keys, constants and requirements.
- No new dependency packages or target frameworks are introduced.

## Tradeoffs

The breaking binary change was deliberately accepted. A single project with subfolders was rejected because it still allowed domain classes to call application policy and vendor assemblies through internal references. Creating service abstractions for every DTO was rejected: ports exist for real replaceable dependencies, not all domain objects.

## Verification

[PR #96](https://github.com/infoconex/RecordSift/pull/96) records test-first Red [docs #41096778484](https://github.com/infoconex/RecordSift/actions/runs/41096778484), which failed the original flat source-tree expectation, and [Green Windows #41098042925](https://github.com/infoconex/RecordSift/actions/runs/41098042925), which compiled all three independent net48 production projects, passed **72/72 core unit tests + 48/48 real adapter tests (120 total)**, Roslyn IDE1006 and package vulnerability audit. [Green docs #41098042892](https://github.com/infoconex/RecordSift/actions/runs/41098042892) passed architecture graph and negative fixtures, source resources, constants and requirement validation. FR end-to-end typed importer acceptance remains separately tracked.
