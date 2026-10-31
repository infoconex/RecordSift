# ADR 0023 — Explicit format-neutral source-to-property mapping

**Status:** Accepted for the release 0.1 FR-010 destination scope, completed through [PR #115](https://github.com/infoconex/RecordSift/pull/115), [PR #116](https://github.com/infoconex/RecordSift/pull/116), and [PR #117](https://github.com/infoconex/RecordSift/pull/117). Immutable models and arbitrary converters are not part of FR-010.

## Context

CSV or future XLSX headers can differ from destination CLR property names. A caller-supplied mapping delegate alone cannot validate destination configuration in advance. FR-010 requires explicit source-to-member declarations and ambiguity rejection; FR-011 provides culture-explicit supported primitive conversion. The mapping contract must remain independent of third-party parsers.

## Decision

1. Domain.Schema owns immutable `SourcePropertyMapping` and `PropertyMappingSchema` declarations. A mapping must name a source in the supplied `ImportSchema`. Duplicate destination member names are rejected by ordinal comparison before any rows are imported. One source may populate multiple distinct properties by explicit declaration.
2. Application.Mapping owns `PrimitivePropertyMapper<T>` as the primary implementation for public writable, non-indexed instance properties on reference-type models with public parameterless constructors. Supported members are `string`, `int`, `decimal`, `DateTime`, `bool` and their supported nullable variants. Unsupported, absent, read-only or incorrectly cased destination properties cause resource-backed configuration exceptions before import. Value-type and immutable destination models are excluded from this release 0.1 contract.
3. `StringPropertyMapper<T>` remains a small compatibility wrapper over `PrimitivePropertyMapper<T>` with its historical string-only destination restriction. It avoids breaking callers; no second mapping engine or additional interface is introduced.
4. Source and destination identifiers use `StringComparer.Ordinal` independently of the comparison behavior of any caller-provided dictionary. Required missing source columns are rejected; unavailable optional source columns leave properties unset. Required *values* inside present columns are a separate FR-017 validation rule.
5. Mapping returns typed record instances using Domain conversion rules. Conversion failures identify the original source column through `FieldConversionException` and are translated by import orchestration into sanitized record diagnostics. Invalid source/header structure remains a source-level failure. No vendor exception or reference enters Domain/Application.

## Limitations and future evolution

At the time of this ADR, XLSX integration was a future workflow. Release 0.1 subsequently added it with independent real-reader acceptance tests documented in [traceability](../../testing/traceability.md). Constructors without public parameterless signatures, immutable destination models, nested properties, custom converters, aliases, and advanced member conventions are not promised by this decision. FR-026 independently tracks the typed-materialization outcome and any approved future destination expansion. Keep validation rules separate from source-header presence.
