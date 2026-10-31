# ADR 0034: Avoid public compile-time constants in reusable library APIs

**Status:** Accepted for release 0.1 API hardening.  
**Issue:** [#146](https://github.com/infoconex/RecordSift/issues/146)

## Context

C# inlines externally referenced `const` values into consuming assemblies. Updating a DLL therefore does not update values embedded in previously compiled clients. PascalCase naming for constant identifiers does not address this binary versioning behavior.

RecordSift exposed 16 Domain diagnostic codes, one CSV default delimiter, and three XLSX read-budget defaults as public `const` fields.

## Decision

Expose those public values as PascalCase `public static readonly` fields. Keep private `const` declarations where they are useful internal implementation details. Consumers obtain public values from the loaded assembly rather than baking values into their code.

Keep exact diagnostic wire identifiers (including `WORKSHEET_NOT_FOUND`), matching invariant resource keys and safe message texts, the default comma delimiter, and the existing XLSX resource limits. Domain diagnostic lookup validates known codes before retrieving its Domain-owned resource.

Establish reflection-based NUnit unit tests that reject any public literal fields in exported Domain and Application types, and verify expected values and resource contracts. Do not add dependencies to core test assemblies.

## Compatibility tradeoff

Converting a public `const` field to `static readonly` **is a source break** for clients using it in `switch case` labels, attribute arguments, or other compile-time constant contexts. Existing compiled clients that had inlined the original values continue to hold those original values; because the value strings and numbers are preserved, runtime behavior remains the same for those clients. New callers can no longer rely on these members as compile-time expressions. Make this migration before release 0.1 rather than silently introduce post-release source incompatibilities.

The public member identifiers retain their names, and no previously defined diagnostic code, structured-result shape, or message changes.

## Verification

The [Windows RED run](https://github.com/infoconex/RecordSift/actions/runs/41178603373) demonstrated the rejected existing public literal API (Domain test failed by design). Final GREEN Windows and documentation CI evidence are captured in [PR #147](https://github.com/infoconex/RecordSift/pull/147).
