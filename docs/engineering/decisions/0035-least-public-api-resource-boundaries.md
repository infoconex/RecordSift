# ADR 0035: Least-accessible configuration defaults and resource boundaries

**Status:** Accepted for release 0.1 API hardening.  
**Issue:** [#148](https://github.com/infoconex/RecordSift/issues/148)  
**Supersedes:** Public visibility of four default fields in [ADR 0034](0034-public-library-constants.md); the no-public-const rule remains in force.

## Context

Public `static readonly` avoids cross-assembly constant inlining, but needlessly exposing default-value fields enlarges the library API. Callers already access CSV and XLSX defaults through typed `Default` option instances.

The Domain also exposes the diagnostic classification implementation methods `IsKnown`, `IsSourceCode`, and `IsRecordCode`. Domain validation requires these methods, but consumers only need stable public diagnostic identifiers and the returned diagnostic `Code`, `Scope`, and `Message`.

## Decision

Use **least necessary accessibility** independently of whether a field is `const` or `static readonly`:

- Keep the 16 Domain `DiagnosticCodes` values public, PascalCase, and `static readonly` because client code can branch on returned codes. Preserve every established diagnostic identifier byte for byte.
- Keep `DiagnosticCodes.IsKnown`, `IsSourceCode` and `IsRecordCode` **internal** to the Domain assembly. Do not expose internals to another production assembly or tests.
- Make `CsvReadOptions.DefaultDelimiter` and three `XlsxReadLimits.DefaultMax*` fields **private const**. Their effective values remain publicly available via `CsvReadOptions.Default.Delimiter` and `XlsxReadLimits.Default` properties.
- Retain explicit API contracts and stable behavior for all imports. Removing previously public members is an intentional source/binary API change before the first 0.1 release, and does not alter wire values, messages, parsing, or validation.

## String/resource policy

Place **human-facing messages** in the layer that owns their meaning: Domain diagnostics/business-rule errors in `DomainMessages.resx`, Application outcome/validation messages in `CsvMessages.resx`, and readable presentation templates in Reporting's `ReportingMessages.resx`. Use typed resource accessors; diagnostics use invariant trusted messages as part of the current contract.

Do **not** place technical string identifiers into translatable message resources: diagnostic wire codes (e.g., `WORKSHEET_NOT_FOUND`), JSON property names, schema versions, resource lookup keys, CSV delimiters, or XLSX OpenXML file paths and package markers. These values have fixed protocol or technical semantics and must not vary by UI culture.

User-provided worksheet and field values remain diagnostic metadata only; never construct trusted error messages from untrusted values.

## Verification

Reflection-based Domain/Application tests reject redundant public default fields and publicly exposed diagnostic classification helpers, while value and resource tests assert unchanged defaults, codes and messages. Check full Windows .NET Framework 4.8 CI and documentation validation prior to merging.
