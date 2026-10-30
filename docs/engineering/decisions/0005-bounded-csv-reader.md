# ADR 0005 — Bounded raw CSV tokenization foundation (superseded)

**Status:** Superseded by [ADR 0008](0008-csvhelper-isolated-adapter.md) under [issue #85](https://github.com/infoconex/RecordSift/issues/85).

The original decision in [PR #82](https://github.com/infoconex/RecordSift/pull/82) implemented comma-separated parsing using a handwritten `CsvRecordReader`. It supported quoted delimiters, multiline records, double-quote escaping, explicit character budgets, and failure-atomic results.

The custom parser was useful as a temporary FR-001 proof of concept, but it was **not** the approved long-term architecture: CSV grammar and vendor decisions should not be owned by the RecordSift domain. ADR 0008 replaces the parser with a library-backed adapter and domain-owned `ICsvRecordReader` contract. Existing component tests remain as characterization tests where applicable. Changes in parsing edge cases require documented evidence, not silent acceptance. FR-001 #2 full typed-import acceptance remains open.
