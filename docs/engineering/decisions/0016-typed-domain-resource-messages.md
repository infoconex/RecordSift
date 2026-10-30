# ADR 0016 — Typed Domain resource-backed messages

**Historical ownership note:** [ADR 0020](0020-application-csv-message-ownership.md) subsequently moved five CSV-only argument messages and their named accessors to Application. The 35/24 counts and code examples below document the original #101 migration, not the current split.

**Status:** Accepted through [engineering issue #101](https://github.com/infoconex/RecordSift/issues/101), following [ADR 0015](0015-layer-aligned-public-namespaces.md). This refines the public resource accessor introduced by [ADR 0010](0010-domain-message-resources.md).

## Context

The Domain owns diagnostic codes and invariant messages, but Application and other Domain callers previously passed raw string keys, such as `"Argument_ReaderHeaderConflict"`, into a public `DomainMessageResources.Get(string resourceKey)`. Those implementation-only identifiers were scattered across source and were not protected by compiler name checking.

## Decision

- Keep all 35 existing messages in `src/RecordSift.Domain/Diagnostics/Resources/DomainMessages.resx`, with stable logical manifest `RecordSift.Resources.DomainMessages.resources`. Diagnostic codes and safe invariant text do not change.
- Expose a **PascalCase public named string property** on `RecordSift.Domain.Diagnostics.DomainMessageResources` for each of the 24 `Argument_*` entries (e.g. `ReaderHeaderConflict`, `DiagnosticRowOutOfRange`, `FailedTableNoError`); each property maps internally to `Argument_<PropertyName>`.
- Restrict `Get(string resourceKey)` to `internal`; core diagnostic code mappings may still use it with their Domain-owned constants. Only the catalog knows raw `Argument_*` resource keys.
- Application and other Domain types consume named Domain message properties; Infrastructure continues converting parser outcomes to core diagnostics without importing vendor types or leaking raw source values.

Use `DomainMessageResources.ReaderHeaderConflict`, not `DomainMessageResources.Get("Argument_ReaderHeaderConflict")`. No generic utility/Helper, DI container, new vendor package or new runtime is needed.

## Compatibility and invariants

This is an intentional pre-release **source and binary API break** removing the public raw-string lookup. Consumers previously calling `Get("Argument_Foo")` must use the `Foo` public property or core diagnostics. Exception types, parameter names, literal resource messages, invariant culture lookup, error codes/scopes, physical/source provenance, report JSON, caller ownership, input bounds, and CSV adapter behavior are unchanged. Missing resources still fail loudly. C# 7.3 and net48 remain the target.

## Verification

`scripts/validate_core_messages.py` verifies each of 24 `Argument_*` entries has one correctly named public accessor; prohibits public `Get(string)`; rejects raw resource-key strings outside the catalog across Domain and Application; includes negative self-tests for old API and raw lookup; and still checks registered diagnostic resources and disallows embedded English production prose.

`DomainMessageResourceTests` verifies the absence of a public key lookup, all 24 property return values against embedded `.resx`, and deterministic behavior under another UI culture. Existing contract tests verify 11 stable diagnostic values/messages. Real-adapter tests remain independent and exercise the unchanged parser-to-domain translation.

Red and Green evidence is attached to [PR #102](https://github.com/infoconex/RecordSift/pull/102). Squash merge requires observed green Windows .NET Framework 4.8 MSBuild, both NUnit suites, Roslyn style, independent NuGet audit, architecture and docs validations. This refactoring does not claim full importer acceptance.
