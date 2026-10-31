# ADR 0023 — Explicit format-neutral source-to-string-property mapping

**Status:** Initial implementation slice under [FR-010 #19](https://github.com/infoconex/RecordSift/issues/19), [PR #115](https://github.com/infoconex/RecordSift/pull/115). This does not complete the full FR-010, FR-011 or FR-026 requirements.

## Context

The original typed CSV use case required a caller-provided mapping delegate, preventing independently configured destination property binding. FR-010 requires source names such as `Customer Id` to assign to differently named .NET properties such as `CustomerId`, and ambiguity must fail before record processing. FR-011 will independently define conversion of integers, decimal, dates, booleans and nullability.

## Decision

1. Domain.Schema owns immutable `SourcePropertyMapping` and `PropertyMappingSchema` declarations. The latter requires at least one mapping, validates each named source against the supplied `ImportSchema`, rejects null declarations and duplicate destination member names, and uses `StringComparer.Ordinal` for deterministic, exact-case identifiers. A source may deliberately populate more than one distinct destination property.
2. Application.Mapping owns `StringPropertyMapper<T>` for this first slice. It accepts only public writable, non-indexed, instance `string` properties of reference-type record models with public parameterless constructors. Value-type targets are excluded to avoid reflection updates to boxed struct copies. Invalid destination configuration throws a resource-backed ArgumentException before reading input. Non-string types are rejected rather than silently converted. The mapper can be passed as `CsvImporter<T>`'s owned delegate, without requiring CSV-specific changes or reverse dependencies.
3. A required mapped source absent from the supplied row is an invalid mapping precondition, never a silently accepted partially populated object. An unavailable optional source leaves the destination unset. Existing CSV header/schema validation rejects missing required source columns before any data rows are mapped.
4. All validation prose remains in Domain's typed `DomainMessageResources` catalog; no parser/vendor exceptions, packages or identifiers leak inward. String setters are only invoked on caller-selected types and properties, and imported raw values are not embedded in exception text.

## Limitations and future evolution

Generalized CLR type conversion, explicit cultures, immutable model constructors, nested properties, aliases, and advanced member conventions are out of scope. They belong to FR-011, FR-026 and related requirements. Mapping names are ordinal/exact-case, not ambient-culture-sensitive. Domain source mapping declarations remain format-independent and reusable by future XLSX Application workflows. FR-010 remains open until the final approved destination-member scope is demonstrably accepted with real source integration.
