# ADR 0003 — Minimal deterministic source-column schema binding

**Decision:** FR-009 introduces immutable `ExpectedColumn`, `ImportSchema`, `SourceColumnBinding` and `SchemaBindingResult` contracts for header-name binding, independent of input format.

## Motivation and boundaries

A CSV or XLSX reader will eventually provide source header metadata to a reusable schema binder. This issue does **not** parse CSV/XLSX, perform property mapping (FR-010), validate values (FR-012/FR-017), support aliases (FR-014), or implement positional mapping (FR-013). Missing required *source columns* are fatal schema-binding errors rather than rejected data rows.

## Choices and consequences

1. Explicit declaration: each column has a nonblank `Name` and `IsRequired`. Missing optional columns produce neither errors nor warnings; no per-row required-field semantics are inferred.
2. Matching is **ordinal, case-sensitive**, culture-independent. No invisible case folding or trimming of source headers. Future configurability requires separate documented behavior/tests.
3. Duplicate *declarations* are configuration errors and throw; duplicate *source headers* for a declared column result in a `AMBIGUOUS_COLUMN` source-level diagnostic, even for optional declarations.
4. Undeclared source headers, including duplicates, are ignored in release 0.1 binding. Blank/null header strings cannot match a valid declaration. No implicit source ordering is assumed.
5. A missing required declared column produces `REQUIRED_COLUMN_MISSING`, a source-level diagnostic with its declared field identifier and null logical row. Multiple problems are reported in schema declaration order. A failed binding does not expose partial column mappings.
6. Successful bindings retain declaration order; `SourceIndex` is explicitly **zero-based** and represents physical column position, not one-based data-row numbers. Source readers retain responsibility for actual physical line and row coordinates.
7. Immutable declaration and result snapshots prevent collection mutation from changing observed binding behavior. The binder accepts in-memory header metadata only; it does not claim FR-009 CSV/XLSX end-to-end acceptance.
8. The two new codes extend FR-028's stable registry in the owning FR-009 implementation, without ad-hoc arbitrary codes. Fixed registry templates are used for source errors.

## Alternatives

- Auto-ignore duplicate matched source headers: rejected as silent ambiguity.
- Case-insensitive matching by default: rejected pending an explicit contract decision and cross-reader comparison.
- Reject every unexpected source column: rejected as unnecessarily restrictive for feeds with additional metadata.
- Eagerly build CSV/headerless parsers or positional model: rejected as FR-001/FR-004/FR-013 scope.

## Verification

NUnit tests exercise declaration validation, correct recognition, required/optional missing columns, ambiguity, deterministic positions, input order, source-only diagnostics, and immutable collections. Tests operate on **supplied headers**: reader integration remains a separate acceptance obligation. See [traceability](../../testing/traceability.md). 
