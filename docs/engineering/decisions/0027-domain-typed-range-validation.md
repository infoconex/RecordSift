# ADR 0027 — Typed Domain range validation after conversion

**Decision:** Implement [FR-020](../../scenarios/functional/FR-020.md) in [issue #21](https://github.com/infoconex/RecordSift/issues/21) using Domain-owned `FieldRangeRule<T>` declarations with explicit strongly typed numeric or date selectors. The Application importer invokes rules only after successful CLR mapping.

## Contract and scope

- `FieldRangeRule<T>.Numeric` uses `decimal` values and bounds, matching the supported lossless primitive numeric conversion policy for `int` and `decimal`. Selectors for integer properties may be promoted to `decimal` without changing precision.
- `FieldRangeRule<T>.Date` uses `DateTime` values, compared by `DateTime.Compare` with no ambient timezone conversion, adjustment or guessing. Source decoding and culture-sensitive conversion remain separate. Consumers must supply comparable DateTime representations and choose consistent timezone policy explicitly.
- Lower and upper bounds may be absent independently, but not both. Each bound declares whether equality is accepted; inclusive and exclusive boundaries are supported. Reversed intervals and equal endpoints with any exclusive boundary are invalid configuration.
- A rule identifies a trusted source field name. On violation, Domain returns the stable record-scoped `FIELD_RANGE_OUT_OF_RANGE` diagnostic, logical row, field and reliable physical line. Resource-backed safe messages never contain raw values, selectors or exception details.
- `CsvImporter<T>` retains its original two-argument constructor for compatibility and provides an overload accepting Domain range rules. Application snapshots configured rules and evaluates them **after successful type conversion** and before adding a record to accepted results.
- If conversion fails, no range rules run. Two independent range failures on the same logical row yield two diagnostics but one rejected record. The range rules are defined in Domain and can be reused by later format importers without third-party coupling.
- Invalid range settings fail at declaration time. Null rule entries are invalid; an omitted rule collection performs no additional validation. No XLSX implementation or cross-field rule behavior is added.

## Evidence

[PR #138](https://github.com/infoconex/RecordSift/pull/138) provides independent FR-020-SC01/SC02 typed real-CSV acceptance and Domain inclusive/exclusive/invalid-configuration tests. The [Windows RED run #41168756129](https://github.com/infoconex/RecordSift/actions/runs/41168756129) demonstrated the missing range rule and stable diagnostic contracts. Promote scenarios only after passing complete Windows NUnit and documentation validation, with exact run links in [traceability](../../testing/traceability.md).
