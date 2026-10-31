# ADR 0010 — Core-owned resource messages and deterministic defaults

**Status:** Accepted through [engineering issue #89](https://github.com/infoconex/RecordSift/issues/89); historical .NET Framework 4.8 / C# 7.3 baseline.

## Problem

The first RecordSift imports correctly translated CsvHelper faults into domain-owned diagnostic codes, but `DiagnosticCodes.GetSafeMessage` and argument validation had inline English message literals. Adapter implementation also repeated the CSV comma delimiter default. Scattered text creates accidental contract drift and prevents later resource presentation.

## Decision

1. The **domain/core** defines every stable error code, diagnostic scope, safe invariant default message, neutral input option, validation and result contract. Third-party adapters **map** exceptions and values to those contracts, without importing vendor type names or raw exception messages.
2. Store **all existing diagnostic messages and argument-validation text** in `src/RecordSift.Domain/Diagnostics/Resources/DomainMessages.resx` embedded in the core assembly. Use `ResourceManager.GetString(code, CultureInfo.InvariantCulture)`. The message remains a safe, deterministic English default for the release 0.1 observable contract, not a culture-dependent UI string. A future presentation layer can localize separately while retaining stable machine codes.
3. Existing `DiagnosticCodes` string values, registry membership, message wording, scopes, and public result/JSON expectations are unchanged. A missing message resource fails immediately; it is not silently replaced by an empty value.
4. The standard CSV default is `CsvReadOptions.Default`/`CsvReadOptions.DefaultDelimiter` in core, consumed by the real CsvHelper adapter. No duplicate delimiter default in vendor configuration.
5. Resource keys are symbolic lookup identifiers and stable diagnostic codes, **not user-facing prose**. Protocol syntax characters and one-based/zero-based indices should be used clearly at their point of meaning; do not generate a meaningless constants abstraction for every literal.
6. Use a dependency-free `scripts/validate_core_messages.py` validation check on CI, with NUnit regressions verifying resource embedding, complete registered codes, exact historical messages and invariant-culture behavior.

The original `DomainMessageResources.Get(string)` public lookup was later deliberately restricted to internal use by [ADR 0016](0016-typed-domain-resource-messages.md). Domain/Application callers now use named, resource-backed properties rather than raw `Argument_*` identifiers. Diagnostic codes, resource text and manifest remain unchanged.

## Boundary and testing

The **core unit-test contract must not require any third-party implementation**. Resource tests use the core assembly and NUnit only; adapter conformance is tested separately with real CsvHelper. At the time of the resource-message PR (#90), a single NUnit test assembly still referenced the adapter. [#91](https://github.com/infoconex/RecordSift/issues/91) subsequently split independent core and adapter test assemblies and introduced vendor-neutral reusable conformance cases; see [ADR 0011](0011-independent-core-contract-tests.md).

## Alternatives and tradeoffs

- Inline switch-based message strings: rejected as scattered and hard to review.
- Vendor-provided exception strings: rejected for untrusted input exposure and loss of deterministic outputs.
- Global mutable culture settings and implicit localization: rejected in this release because output needs deterministic message text; explicit display localization can be added later without changing codes.
- Moving all syntax or row-number constants to a giant shared constants class: rejected as noise without encapsulation value.

## Verification

[PR #90](https://github.com/infoconex/RecordSift/pull/90) records TDD Red, Green, documentation and Windows net48 evidence. Separate #91 enforces domain-only unit-test assembly isolation.
