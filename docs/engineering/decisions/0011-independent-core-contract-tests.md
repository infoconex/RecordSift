# ADR 0011 — Independent domain tests and shared adapter conformance

**Status:** Accepted design in [engineering #91](https://github.com/infoconex/RecordSift/issues/91), implementing [ADR 0007](0007-domain-ports-and-adapters.md).

## Motivation

A core-owned C# interface alone does not guarantee replaceability. The initial single test assembly linked to CsvHelper, so even pure domain unit tests were forced to compile vendor assemblies and could not independently exercise the domain contract.

## Decision

- The **core is authoritative** for `ICsvRecordReader` input and output types, options, deterministic header/record policy, source provenance, error scope/codes/resource messages, resource limits, caller input ownership and failure atomicity. No adapter can redefine these semantics.
- `RecordSift.Tests` is a self-contained core NUnit assembly referencing only `RecordSift` and the NUnit test framework. It contains fake-driven unit tests and the reusable **abstract** `CsvRecordReaderContractTests` fixture. That fixture defines behavior entirely in core-owned types without importing CsvHelper.
- Each real adapter owns its own NUnit assembly; `RecordSift.CsvHelperAdapter.Tests` inherits the exact reader behavioral contract fixture as `CsvHelperContractConformanceTests`, creates a real adapter instance, and also tests vendor edge cases and table integration. New parser implementation = new adapter assembly and contract subclass, not rewritten domain tests.
- The Windows solution restores/builds both NUnit assemblies and runs them separately, publishing two XML result sets. The dependency audit checks that core-test project references and source imports never point at adapter or parser vendor assemblies. Core-test binary output is checked not to contain adapter DLLs.
- NUnit is a **test framework dependency only**. The concern is third-party importer dependencies in unit-test authoring and core compilation.
- This split is not a statement that all full FR-001 typed import acceptance scenarios exist; those remain independently tracked.

## Tradeoffs

A test assembly reference from the adapter-test assembly to the core test assembly allows direct inheritance of contract fixtures without copying test code or creating a new testing framework. Core tests remain independently runnable. If additional adapters grow, consider a dedicated vendor-independent contract-test support library, but do not add one preemptively.

## Verification

[PR #92](https://github.com/infoconex/RecordSift/pull/92) records observed Red: [Windows #41084499309](https://github.com/infoconex/RecordSift/actions/runs/41084499309) compiled 119 tests but failed the new domain-unit-test assembly isolation assertion because it referenced `RecordSift.CsvHelperAdapter` (118 passed, 1 failed). **Observed Green:** [Windows #41085050502](https://github.com/infoconex/RecordSift/actions/runs/41085050502) built the traditional net48 solution and ran the two assemblies separately: **71 core tests passed**, **48 real adapter contract/component tests passed**, zero failed or skipped; package inventory and NuGet audit passed. Documentation validation passed in [workflow #41085050474](https://github.com/infoconex/RecordSift/actions/runs/41085050474). The conformance suite is consumer-owned; a future parser cannot weaken expected semantics.
