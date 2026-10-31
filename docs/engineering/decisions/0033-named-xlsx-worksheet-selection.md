# ADR 0033: Exact named worksheet selection for XLSX imports

**Status:** Accepted for FR-005 implementation.
**Issue:** [#18](https://github.com/infoconex/RecordSift/issues/18)
**Requirements:** [FR-005](../../scenarios/functional/FR-005.md), [FR-002](../../scenarios/functional/FR-002.md)

## Context

FR-002 imports the first worksheet and sends its source rows through the same Application validation pipeline as CSV. Multi-sheet workbooks may contain unrelated records. Selection must not change schema binding, type conversion, row accounting, CSV behavior, or infrastructure boundaries.

## Decision

Add an optional `worksheetName` parameter to the Application `XlsxImporter<T>` and `IXlsxWorksheetReader` contract. The ExcelDataReader adapter advances workbook result sets until the requested exact, ordinal, case-sensitive worksheet name matches. When no selector is supplied, the existing first-sheet behavior remains unchanged. Blank or whitespace-only explicit names are invalid caller arguments. Selecting an absent name yields the Domain-registered, resource-backed source diagnostic `WORKSHEET_NOT_FOUND`, not a fallback or a successful empty import.

A selected sheet is read in isolation; unselected sheet rows never reach schema binding, validation, or result accounting. Every selected-sheet record diagnostic retains the actual name and one-based physical row. A hidden sheet remains selectable by its stored name; empty sheets retain existing header-validation behavior. Name-based selection is the release 0.1 contract; index selection and cross-sheet aggregation are out of scope.

## Consequences

The format-neutral validation pipeline and immutable result contracts remain unchanged. XLSX vendor types remain confined to Infrastructure; no Excel automation, formula evaluation, or additional dependency is introduced. Stable diagnostics do not include untrusted requested worksheet names in message text.

[Windows RED #41177821726](https://github.com/infoconex/RecordSift/actions/runs/41177821726) established missing contracts; [Windows GREEN #41177928686](https://github.com/infoconex/RecordSift/actions/runs/41177928686) verified 258/258 tests, including real multi-sheet acceptance and the CSV regression suite. Additional boundary tests cover exact matching and invalid blank selectors.
