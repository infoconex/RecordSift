# ADR 0028 — Per-import typed identifier duplicate detection

**Decision:** Implement [FR-021](../../scenarios/functional/FR-021.md) in [issue #22](https://github.com/infoconex/RecordSift/issues/22) using an immutable Domain-owned `DuplicateIdentifierRule<T>` declaration and a fresh validator session for each Application import.

## Contract and equality

- `DuplicateIdentifierRule<T>.ForKey<TKey>(field, selector, comparer)` accepts a typed identifier selector and an optional `IEqualityComparer<TKey>`. The default is `EqualityComparer<TKey>.Default`. Strings can explicitly use ordinal case-sensitive or ordinal case-insensitive equality without ambient culture.
- Source column and CLR property types are not conflated; the caller declares which typed property is the identity and the trusted source field name used by diagnostics. Null keys are not implicitly required or stored. Required identifier presence and nonblank source values remain owned by FR-012 and FR-017.
- The Domain rule is immutable. `CreateValidator()` constructs a new `HashSet<TKey>` and returns an import-scoped function that checks and remembers successfully accepted identifier keys. The first accepted record wins. Later equal identifiers are rejected.
- Application invokes the checker after required-field, length, conversion and numeric/date range validation. Records failing earlier checks never enter the accepted-key set; a duplicate is reported only for an otherwise acceptable record.
- A duplicate yields one record-scoped `DUPLICATE_IDENTIFIER` error with logical row, reliable physical line and trusted identifier field. Its fixed resource-backed message never includes a key, raw source value or private record content.
- The original two- and three-argument `CsvImporter<T>` constructors remain unchanged; a new four-argument overload accepts the optional typed identifier policy. Each `Import` invocation obtains its own checker, so a reused importer does not share past record identities.
- The per-import set uses memory proportional to accepted distinct identifiers; the existing CSV character-budget limit bounds the input size. Cross-file history, external databases, compound keys and streaming optimization are not introduced.

## Evidence

[PR #139](https://github.com/infoconex/RecordSift/pull/139) contains FR-021-SC01/SC02 real typed CSV acceptance and Domain equality/session tests. [Windows RED #41169411656](https://github.com/infoconex/RecordSift/actions/runs/41169411656) demonstrated the missing Domain and importer contracts. Promote scenarios in [traceability](../../testing/traceability.md) only after the complete Windows CI suite succeeds.
