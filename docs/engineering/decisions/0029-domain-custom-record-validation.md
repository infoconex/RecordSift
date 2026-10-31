# ADR 0029 — Domain-owned custom record validation

**Status:** Accepted for release 0.1  
**Issue:** [#23](https://github.com/infoconex/RecordSift/issues/23)  
**Requirement:** [FR-022](../../scenarios/functional/FR-022.md)

## Decision

A `RecordValidationRule<T>` belongs to Domain and accepts an already-converted record through a typed `Func<T, bool>`. Its optional field identifier distinguishes a field-specific failure from a whole-record failure. The returned `ImportDiagnostic` always uses the stable Domain `VALIDATION_FAILED` code and resource-owned, sanitized message. A user-provided message, raw value, file path, or arbitrary diagnostic code is never interpolated.

Rules are snapshotted at importer construction. The Application importer runs them in registration order, after required-value, field-length, conversion, and built-in range checks; it runs duplicate-identifier tracking only after custom rules pass. Each failing rule contributes one diagnostic, but a rejected logical record is counted once, regardless of the number of failed rules. Validated records maintain logical row and reliable physical line provenance.

A rule has access to the converted record but no implicit access to other imported records. If a consumer supplies a closure with external state, ownership and reset are the consumer's responsibility. An exception in a custom delegate propagates as a programming/runtime failure: it must not be incorrectly reported as a source, record-conversion, or validation diagnostic. Rules should therefore be deterministic and exception-free for expected inputs.

## Rationale and consequences

The contract is format-neutral and uses no parser, serializer, third-party validator, or reflection type. It preserves the existing CSV importer constructors through a new overload. Security relies on the Domain diagnostic registry rather than untrusted rule messages. Richer custom diagnostics and cross-record validation remain separate contract decisions; no expression language or script execution is introduced.
