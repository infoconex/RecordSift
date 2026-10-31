# ADR 0024 — Culture-explicit primitive conversion and source-aware typed mapping

**Status:** Accepted through [FR-011 #4](https://github.com/infoconex/RecordSift/issues/4), [PR #116](https://github.com/infoconex/RecordSift/pull/116). Builds on [ADR 0023](0023-explicit-string-property-mapping.md).

## Context

FR-011 needs deterministic conversion into supported CLR types. Ambient culture and parser-vendor conversion settings cannot define Domain behavior. Raw input values and vendor exception messages must never leak into public diagnostics. The existing string-only mapper contract must remain valid.

## Decision

- Domain.Schema owns `PrimitiveFieldConverter`, a vendor-neutral converter using an explicit, defensively cloned read-only `CultureInfo`. Supported targets are `string`, `int`, `decimal`, `DateTime`, `bool`, and nullable forms of supported value types.
- Integer parsing uses `NumberStyles.Integer`, decimal uses `NumberStyles.Number`, date parsing uses culture-explicit `DateTime.TryParse` with `DateTimeStyles.None`, and booleans use standard .NET true/false tokens. No ambient-culture fallback or implicit rounding. Empty/whitespace values become null for nullable value types; blank/invalid nonnullable primitives fail. Strings remain unchanged.
- The FR-018 acceptance hardening enforces **exact mathematical decimal value preservation** after `decimal.TryParse`: compare the normalized, culture-specific source representation against a full fixed-point representation of the parsed decimal. A long fraction that the framework would otherwise silently round is rejected as a safe format failure. Accepted grouping, signs and trailing fractional zeros remain numerically equivalent. This adds no third-party or outward Domain dependencies; no raw input is exposed through diagnostics. See [FR-018 #13](https://github.com/infoconex/RecordSift/issues/13) and [PR #121](https://github.com/infoconex/RecordSift/pull/121).
- Application.Mapping owns `PrimitivePropertyMapper<T>` and composes Domain mapping declarations and primitive conversion, validating writable supported CLR members in advance. Reference models with public parameterless constructors are required, preventing boxed value-type mutations. Existing `StringPropertyMapper<T>` delegates to the same implementation in string-only mode.
- A writable property setter can reject an otherwise converted value. The mapper translates the reflection `TargetInvocationException` into a sanitized field-specific `FieldConversionException` so one bad record does not terminate a valid batch; neither a setter's thrown message nor the raw source value crosses the public diagnostic boundary. Unsupported/read-only/indexer destinations are rejected at mapping construction.
- Domain.Diagnostics owns `FieldConversionException`, containing only a declared source column and safe resource-backed text. Application `CsvImporter<T>` translates that exception into `ConversionFailed` with logical data-row index, physical CSV line when reliable, and original field identifier. No raw values, paths, stack traces or parser exception strings appear in the result.
- The CSV vendor remains Infrastructure-only; Domain and Application retain approved framework references and independent fake-port tests. Real CsvHelper integration exercises the public typed import route.

## Deferred scope

Custom converters, enum/Guid/floating-point types, exact date-format lists, constructor binding, nested models, advanced nullable/optional field validation and JSON reporting remain separate requirements. FR-011 conversion acceptance does not imply that FR-001, FR-017 or FR-026 are fully accepted.
