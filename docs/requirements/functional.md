# Functional requirements — RecordSift

**Baseline:** Draft for release 0.1. The library is not yet implemented.

**Scope:** Import CSV and XLSX into strongly typed records using a common schema, validate those records, and return consistent diagnostics plus JSON reports. Distinguish errors in source records from fatal failures to read a file.

**Priority:** Must = intended release 0.1; Should = optional backlog; Future = deferred.

| Requirement | Behavior | Priority | GitHub issue / acceptance |
| --- | --- | --- | --- |
| FR-001 | Import CSV files | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) (`FR-001-SC01`) |
| FR-002 | Import XLSX workbooks | Must | [#6](https://github.com/infoconex/RecordSift/issues/6) (`FR-002-SC01`) |
| FR-003 | Configure CSV delimiters | Must | [#10](https://github.com/infoconex/RecordSift/issues/10) (`FR-003-SC01`) |
| FR-004 | Support CSV with/without headers | Must | [#14](https://github.com/infoconex/RecordSift/issues/14) (`FR-004-SC01`) |
| FR-005 | Select Excel worksheets | Must | [#18](https://github.com/infoconex/RecordSift/issues/18) (`FR-005-SC01`) |
| FR-006 | Configure CSV text encoding | Must | [#3](https://github.com/infoconex/RecordSift/issues/3) (`FR-006-SC01`) |
| FR-007 | Read fixed-width text files | Future | [#7](https://github.com/infoconex/RecordSift/issues/7) (`FR-007-SC01`) |
| FR-008 | Extensible input format interfaces | Future | [#11](https://github.com/infoconex/RecordSift/issues/11) (`FR-008-SC01`) |
| FR-009 | Define expected columns | Must | [#15](https://github.com/infoconex/RecordSift/issues/15) (`FR-009-SC01`) |
| FR-010 | Map columns to .NET properties | Must | [#19](https://github.com/infoconex/RecordSift/issues/19) (`FR-010-SC01`) |
| FR-011 | Convert common .NET types | Must | [#4](https://github.com/infoconex/RecordSift/issues/4) (`FR-011-SC01`) |
| FR-012 | Declare required and optional fields | Must | [#8](https://github.com/infoconex/RecordSift/issues/8) (`FR-012-SC01`) |
| FR-013 | Map columns by name or position | Must | [#12](https://github.com/infoconex/RecordSift/issues/12) (`FR-013-SC01`) |
| FR-014 | Column name aliases | Should | [#16](https://github.com/infoconex/RecordSift/issues/16) (`FR-014-SC01`) |
| FR-015 | Default field values | Should | [#20](https://github.com/infoconex/RecordSift/issues/20) (`FR-015-SC01`) |
| FR-016 | Custom field conversion | Should | [#5](https://github.com/infoconex/RecordSift/issues/5) (`FR-016-SC01`) |
| FR-017 | Required-field validation | Must | [#9](https://github.com/infoconex/RecordSift/issues/9) (`FR-017-SC01`) |
| FR-018 | Data-type conversion validation | Must | [#13](https://github.com/infoconex/RecordSift/issues/13) (`FR-018-SC01`) |
| FR-019 | Field length validation | Must | [#17](https://github.com/infoconex/RecordSift/issues/17) (`FR-019-SC01`) |
| FR-020 | Numeric/date range validation | Must | [#21](https://github.com/infoconex/RecordSift/issues/21) (`FR-020-SC01`) |
| FR-021 | Duplicate identifier detection | Must | [#22](https://github.com/infoconex/RecordSift/issues/22) (`FR-021-SC01`) |
| FR-022 | Custom validation rules | Must | [#23](https://github.com/infoconex/RecordSift/issues/23) (`FR-022-SC01`) |
| FR-023 | Regular-expression validation | Should | [#24](https://github.com/infoconex/RecordSift/issues/24) (`FR-023-SC01`) |
| FR-024 | Cross-field validation | Should | [#25](https://github.com/infoconex/RecordSift/issues/25) (`FR-024-SC01`) |
| FR-025 | Configurable validation severity | Should | [#26](https://github.com/infoconex/RecordSift/issues/26) (`FR-025-SC01`) |
| FR-026 | Strongly typed record import | Must | [#27](https://github.com/infoconex/RecordSift/issues/27) (`FR-026-SC01`) |
| FR-027 | Return accepted records | Must | [#28](https://github.com/infoconex/RecordSift/issues/28) (`FR-027-SC01`) |
| FR-028 | Invalid-record diagnostics | Must | [#29](https://github.com/infoconex/RecordSift/issues/29) (`FR-028-SC01`) |
| FR-029 | Continue-on-error processing | Must | [#30](https://github.com/infoconex/RecordSift/issues/30) (`FR-029-SC01`) |
| FR-030 | Import summary | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) (`FR-030-SC01`) |
| FR-031 | Fail-fast processing | Should | [#32](https://github.com/infoconex/RecordSift/issues/32) (`FR-031-SC01`) |
| FR-032 | Cancellation | Should | [#33](https://github.com/infoconex/RecordSift/issues/33) (`FR-032-SC01`) |
| FR-033 | Asynchronous API | Future | [#34](https://github.com/infoconex/RecordSift/issues/34) (`FR-033-SC01`) |
| FR-034 | Streaming large datasets | Future | [#35](https://github.com/infoconex/RecordSift/issues/35) (`FR-034-SC01`) |
| FR-035 | JSON validation reports | Must | [#36](https://github.com/infoconex/RecordSift/issues/36) (`FR-035-SC01`) |
| FR-036 | Structured in-memory results | Must | [#37](https://github.com/infoconex/RecordSift/issues/37) (`FR-036-SC01`) |
| FR-037 | Human-readable summaries | Must | [#38](https://github.com/infoconex/RecordSift/issues/38) (`FR-037-SC01`) |
| FR-038 | CSV error exports | Should | [#39](https://github.com/infoconex/RecordSift/issues/39) (`FR-038-SC01`) |
| FR-039 | Custom report formatting | Future | [#40](https://github.com/infoconex/RecordSift/issues/40) (`FR-039-SC01`) |

## Acceptance and change control

Each issue contains an independently verifiable scenario and a testing expectation. Implementing another requirement does not automatically close its issue. Cross-cutting quality obligations are documented separately in [nonfunctional requirements](non-functional.md).

Before declaring release 0.1 complete, every **Must** requirement needs an executable passing acceptance test, explicit manual verification evidence where appropriate, or an approved documented change to scope. Feature behavior and dependency versions must not be invented to satisfy an issue.

Any change to source-to-record indexing, row numbering, validation failure semantics, or JSON contracts belongs in the behavioral specification tracked by [#51](https://github.com/infoconex/RecordSift/issues/51).
