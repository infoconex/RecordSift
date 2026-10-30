# Functional requirements — RecordSift

**Baseline:** Release 0.1 is in implementation. FR-030 has an in-memory summary/status model and unit tests; the CSV/XLSX importer and other requirements are not yet implemented.

**Scope:** Import CSV and XLSX into strongly typed records using a common schema, validate those records, and return consistent diagnostics plus JSON reports. Distinguish errors in source records from fatal failures to read a file.

**Priority:** Must = intended release 0.1; Should = optional backlog; Future = deferred.

| Requirement | Behavior | Priority | GitHub issue / acceptance |
| --- | --- | --- | --- |
| FR-001 | Import CSV files | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) (`FR-001-SC01`) · [All scenarios](../scenarios/functional/FR-001.md) |
| FR-002 | Import XLSX workbooks | Must | [#6](https://github.com/infoconex/RecordSift/issues/6) (`FR-002-SC01`) · [All scenarios](../scenarios/functional/FR-002.md) |
| FR-003 | Configure CSV delimiters | Must | [#10](https://github.com/infoconex/RecordSift/issues/10) (`FR-003-SC01`) · [All scenarios](../scenarios/functional/FR-003.md) |
| FR-004 | Support CSV with/without headers | Must | [#14](https://github.com/infoconex/RecordSift/issues/14) (`FR-004-SC01`) · [All scenarios](../scenarios/functional/FR-004.md) |
| FR-005 | Select Excel worksheets | Must | [#18](https://github.com/infoconex/RecordSift/issues/18) (`FR-005-SC01`) · [All scenarios](../scenarios/functional/FR-005.md) |
| FR-006 | Configure CSV text encoding | Must | [#3](https://github.com/infoconex/RecordSift/issues/3) (`FR-006-SC01`) · [All scenarios](../scenarios/functional/FR-006.md) |
| FR-007 | Read fixed-width text files | Future | [#7](https://github.com/infoconex/RecordSift/issues/7) (`FR-007-SC01`) · [All scenarios](../scenarios/functional/FR-007.md) |
| FR-008 | Extensible input format interfaces | Future | [#11](https://github.com/infoconex/RecordSift/issues/11) (`FR-008-SC01`) · [All scenarios](../scenarios/functional/FR-008.md) |
| FR-009 | Define expected columns | Must | [#15](https://github.com/infoconex/RecordSift/issues/15) (`FR-009-SC01`) · [All scenarios](../scenarios/functional/FR-009.md) |
| FR-010 | Map columns to .NET properties | Must | [#19](https://github.com/infoconex/RecordSift/issues/19) (`FR-010-SC01`) · [All scenarios](../scenarios/functional/FR-010.md) |
| FR-011 | Convert common .NET types | Must | [#4](https://github.com/infoconex/RecordSift/issues/4) (`FR-011-SC01`) · [All scenarios](../scenarios/functional/FR-011.md) |
| FR-012 | Declare required and optional fields | Must | [#8](https://github.com/infoconex/RecordSift/issues/8) (`FR-012-SC01`) · [All scenarios](../scenarios/functional/FR-012.md) |
| FR-013 | Map columns by name or position | Must | [#12](https://github.com/infoconex/RecordSift/issues/12) (`FR-013-SC01`) · [All scenarios](../scenarios/functional/FR-013.md) |
| FR-014 | Column name aliases | Should | [#16](https://github.com/infoconex/RecordSift/issues/16) (`FR-014-SC01`) · [All scenarios](../scenarios/functional/FR-014.md) |
| FR-015 | Default field values | Should | [#20](https://github.com/infoconex/RecordSift/issues/20) (`FR-015-SC01`) · [All scenarios](../scenarios/functional/FR-015.md) |
| FR-016 | Custom field conversion | Should | [#5](https://github.com/infoconex/RecordSift/issues/5) (`FR-016-SC01`) · [All scenarios](../scenarios/functional/FR-016.md) |
| FR-017 | Required-field validation | Must | [#9](https://github.com/infoconex/RecordSift/issues/9) (`FR-017-SC01`) · [All scenarios](../scenarios/functional/FR-017.md) |
| FR-018 | Data-type conversion validation | Must | [#13](https://github.com/infoconex/RecordSift/issues/13) (`FR-018-SC01`) · [All scenarios](../scenarios/functional/FR-018.md) |
| FR-019 | Field length validation | Must | [#17](https://github.com/infoconex/RecordSift/issues/17) (`FR-019-SC01`) · [All scenarios](../scenarios/functional/FR-019.md) |
| FR-020 | Numeric/date range validation | Must | [#21](https://github.com/infoconex/RecordSift/issues/21) (`FR-020-SC01`) · [All scenarios](../scenarios/functional/FR-020.md) |
| FR-021 | Duplicate identifier detection | Must | [#22](https://github.com/infoconex/RecordSift/issues/22) (`FR-021-SC01`) · [All scenarios](../scenarios/functional/FR-021.md) |
| FR-022 | Custom validation rules | Must | [#23](https://github.com/infoconex/RecordSift/issues/23) (`FR-022-SC01`) · [All scenarios](../scenarios/functional/FR-022.md) |
| FR-023 | Regular-expression validation | Should | [#24](https://github.com/infoconex/RecordSift/issues/24) (`FR-023-SC01`) · [All scenarios](../scenarios/functional/FR-023.md) |
| FR-024 | Cross-field validation | Should | [#25](https://github.com/infoconex/RecordSift/issues/25) (`FR-024-SC01`) · [All scenarios](../scenarios/functional/FR-024.md) |
| FR-025 | Configurable validation severity (deferred; no warnings in 0.1) | Future | [#26](https://github.com/infoconex/RecordSift/issues/26) (`FR-025-SC01`) · [All scenarios](../scenarios/functional/FR-025.md) |
| FR-026 | Strongly typed record import | Must | [#27](https://github.com/infoconex/RecordSift/issues/27) (`FR-026-SC01`) · [All scenarios](../scenarios/functional/FR-026.md) |
| FR-027 | Return accepted records | Must | [#28](https://github.com/infoconex/RecordSift/issues/28) (`FR-027-SC01`) · [All scenarios](../scenarios/functional/FR-027.md) |
| FR-028 | Invalid-record diagnostics | Must | [#29](https://github.com/infoconex/RecordSift/issues/29) (`FR-028-SC01`) · [All scenarios](../scenarios/functional/FR-028.md) |
| FR-029 | Continue-on-error processing | Must | [#30](https://github.com/infoconex/RecordSift/issues/30) (`FR-029-SC01`) · [All scenarios](../scenarios/functional/FR-029.md) |
| FR-030 | Import summary | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) (`FR-030-SC01`) · [All scenarios](../scenarios/functional/FR-030.md) |
| FR-031 | Fail-fast processing | Should | [#32](https://github.com/infoconex/RecordSift/issues/32) (`FR-031-SC01`) · [All scenarios](../scenarios/functional/FR-031.md) |
| FR-032 | Cancellation | Should | [#33](https://github.com/infoconex/RecordSift/issues/33) (`FR-032-SC01`) · [All scenarios](../scenarios/functional/FR-032.md) |
| FR-033 | Asynchronous API | Future | [#34](https://github.com/infoconex/RecordSift/issues/34) (`FR-033-SC01`) · [All scenarios](../scenarios/functional/FR-033.md) |
| FR-034 | Streaming large datasets | Future | [#35](https://github.com/infoconex/RecordSift/issues/35) (`FR-034-SC01`) · [All scenarios](../scenarios/functional/FR-034.md) |
| FR-035 | JSON validation reports | Must | [#36](https://github.com/infoconex/RecordSift/issues/36) (`FR-035-SC01`) · [All scenarios](../scenarios/functional/FR-035.md) |
| FR-036 | Structured in-memory results | Must | [#37](https://github.com/infoconex/RecordSift/issues/37) (`FR-036-SC01`) · [All scenarios](../scenarios/functional/FR-036.md) |
| FR-037 | Human-readable summaries | Must | [#38](https://github.com/infoconex/RecordSift/issues/38) (`FR-037-SC01`) · [All scenarios](../scenarios/functional/FR-037.md) |
| FR-038 | CSV error exports | Should | [#39](https://github.com/infoconex/RecordSift/issues/39) (`FR-038-SC01`) · [All scenarios](../scenarios/functional/FR-038.md) |
| FR-039 | Custom report formatting | Future | [#40](https://github.com/infoconex/RecordSift/issues/40) (`FR-039-SC01`) · [All scenarios](../scenarios/functional/FR-039.md) |

## Acceptance and change control

Each issue contains an independently verifiable scenario and a testing expectation. Implementing another requirement does not automatically close its issue. Cross-cutting quality obligations are documented separately in [nonfunctional requirements](non-functional.md).

Before declaring release 0.1 complete, every **Must** requirement needs an executable passing acceptance test, explicit manual verification evidence where appropriate, or an approved documented change to scope. Feature behavior and dependency versions must not be invented to satisfy an issue.

Any change to source-to-record indexing, row numbering, validation failure semantics, or JSON contracts belongs in the behavioral specification tracked by [#51](https://github.com/infoconex/RecordSift/issues/51).
