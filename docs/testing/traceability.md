# Scenario-to-test traceability

**Scope:** 49 requirements and 109 acceptance scenarios. [Issue #105](https://github.com/infoconex/RecordSift/issues/105) separates Domain-only and Application-only NUnit assemblies; the current test names for Domain scenario rows are `RecordSift.Domain.Tests.*`. Earlier CI evidence remains historical; no acceptance status changes. Architecture refactor #95 preserves these scenario identifiers; [Windows #41098042925](https://github.com/infoconex/RecordSift/actions/runs/41098042925) verified 72 domain/application tests and 48 real adapter tests (120/120), including new assembly-direction checks. Full typed importer acceptance remains Planned. FR-030 summary-level scenarios SC01–SC05 have automated **in-memory unit** evidence; the remaining 104 scenarios are Planned. FR-028/009/013 contracts, FR-001 parser and FR-004 header-policy components have independently verified core/adapter contract and component tests (**119/119** across two assemblies in [Windows #41085050502](https://github.com/infoconex/RecordSift/actions/runs/41085050502): 71 core and 48 real CsvHelper adapter tests). Only the five FR-030 in-memory scenarios are Automated; the remaining 104 acceptance scenarios remain Planned pending full import integration. This does not establish CSV/XLSX end-to-end behavior.

## Maintenance contract

- The authoritative scenario text lives in `docs/scenarios/functional/` and `docs/scenarios/non-functional/`; GitHub issues track the corresponding requirement's implementation.
- Each scenario has exactly one row below. Update the appropriate row with a real test/check and its observed CI run or reproducible manual evidence during the implementation PR.
- `Planned` means not yet verified; `Automated` and `Manual` require execution evidence; `Blocked` requires a documented reason. Do not infer verification from a closed issue.
- Keep new scenarios in the same requirement-specific Markdown file; give them stable ascending `SC##` IDs and update this matrix in the same PR.
- Release gates are the Must requirements and their approved release-0.1 scenarios; Should/Future remain tracked but are not release blockers unless scope changes.

| Scenario | Requirement | Priority | GitHub issue | Authoritative specification | Test/check | Status | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FR-001-SC01 | FR-001 | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) | [FR-001 scenarios](../scenarios/functional/FR-001.md) | — | Planned | — |
| FR-001-SC02 | FR-001 | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) | [FR-001 scenarios](../scenarios/functional/FR-001.md) | — | Planned | — |
| FR-001-SC03 | FR-001 | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) | [FR-001 scenarios](../scenarios/functional/FR-001.md) | `RecordSift.Infrastructure.Csv.Tests.CsvAdapterContractConformanceTests.FR_001_SC03_UnclosedQuotedFieldIsSourceFailureWithPhysicalLocation` | Planned | [NUnit 119/119 (71 core, 48 adapter), Windows #41085050502](https://github.com/infoconex/RecordSift/actions/runs/41085050502) — adapter inherits core-owned CSV contract fixture; typed source/import result integration pending. |
| FR-001-SC04 | FR-001 | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) | [FR-001 scenarios](../scenarios/functional/FR-001.md) | `RecordSift.Infrastructure.Csv.Tests.CsvAdapterContractConformanceTests.FR_001_SC04_QuotedCommaNewlineAndEscapedQuoteStayInOneField` | Planned | [NUnit 119/119 (71 core, 48 adapter), Windows #41085050502](https://github.com/infoconex/RecordSift/actions/runs/41085050502) — core-owned contract fixture against real CSV adapter; source/import result integration pending. |
| FR-001-SC05 | FR-001 | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) | [FR-001 scenarios](../scenarios/functional/FR-001.md) | — | Planned | — |
| FR-001-SC06 | FR-001 | Must | [#2](https://github.com/infoconex/RecordSift/issues/2) | [FR-001 scenarios](../scenarios/functional/FR-001.md) | — | Planned | — |
| FR-002-SC01 | FR-002 | Must | [#6](https://github.com/infoconex/RecordSift/issues/6) | [FR-002 scenarios](../scenarios/functional/FR-002.md) | — | Planned | — |
| FR-002-SC02 | FR-002 | Must | [#6](https://github.com/infoconex/RecordSift/issues/6) | [FR-002 scenarios](../scenarios/functional/FR-002.md) | — | Planned | — |
| FR-003-SC01 | FR-003 | Must | [#10](https://github.com/infoconex/RecordSift/issues/10) | [FR-003 scenarios](../scenarios/functional/FR-003.md) | `RecordSift.Infrastructure.Csv.Tests.CsvDelimiterTests.FR_003_SC01_SemicolonDelimiterPreservesQuotedSemicolonInRealAdapter` | Planned | [NUnit 112/112, Windows #41081248991](https://github.com/infoconex/RecordSift/actions/runs/41081248991) — real parser plus headed table component; typed importer acceptance pending. |
| FR-003-SC02 | FR-003 | Must | [#10](https://github.com/infoconex/RecordSift/issues/10) | [FR-003 scenarios](../scenarios/functional/FR-003.md) | `RecordSift.Infrastructure.Csv.Tests.CsvDelimiterTests.FR_003_SC02_InvalidDelimiterReturnsSafeSourceDiagnosticBeforeReading`; `InvalidDelimiterFailsInCoreBeforeCallingParserPort` | Planned | [NUnit 112/112, Windows #41081248991](https://github.com/infoconex/RecordSift/actions/runs/41081248991) — source-validation component; public typed importer acceptance pending. |
| FR-004-SC01 | FR-004 | Must | [#14](https://github.com/infoconex/RecordSift/issues/14) | [FR-004 scenarios](../scenarios/functional/FR-004.md) | `RecordSift.Infrastructure.Csv.Tests.CsvHeaderPolicyTests.FR_004_SC01_HeadedAndHeaderlessCsvRetainIdenticalDataRows` | Planned | [NUnit 94/94, Windows #41075489461](https://github.com/infoconex/RecordSift/actions/runs/41075489461) — real CsvHelper adapter/raw-record/header binding component; typed import acceptance pending. |
| FR-004-SC02 | FR-004 | Must | [#14](https://github.com/infoconex/RecordSift/issues/14) | [FR-004 scenarios](../scenarios/functional/FR-004.md) | `RecordSift.Infrastructure.Csv.Tests.CsvHeaderPolicyTests.FR_004_SC02_MissingRequiredCsvHeaderFailsWithoutBindingAnyData` | Planned | [NUnit 94/94, Windows #41075489461](https://github.com/infoconex/RecordSift/actions/runs/41075489461) — real CSV raw-record/header binding component; typed import acceptance pending. |
| FR-005-SC01 | FR-005 | Must | [#18](https://github.com/infoconex/RecordSift/issues/18) | [FR-005 scenarios](../scenarios/functional/FR-005.md) | — | Planned | — |
| FR-005-SC02 | FR-005 | Must | [#18](https://github.com/infoconex/RecordSift/issues/18) | [FR-005 scenarios](../scenarios/functional/FR-005.md) | — | Planned | — |
| FR-006-SC01 | FR-006 | Must | [#3](https://github.com/infoconex/RecordSift/issues/3) | [FR-006 scenarios](../scenarios/functional/FR-006.md) | — | Planned | — |
| FR-006-SC02 | FR-006 | Must | [#3](https://github.com/infoconex/RecordSift/issues/3) | [FR-006 scenarios](../scenarios/functional/FR-006.md) | — | Planned | — |
| FR-007-SC01 | FR-007 | Future | [#7](https://github.com/infoconex/RecordSift/issues/7) | [FR-007 scenarios](../scenarios/functional/FR-007.md) | — | Planned | — |
| FR-007-SC02 | FR-007 | Future | [#7](https://github.com/infoconex/RecordSift/issues/7) | [FR-007 scenarios](../scenarios/functional/FR-007.md) | — | Planned | — |
| FR-008-SC01 | FR-008 | Future | [#11](https://github.com/infoconex/RecordSift/issues/11) | [FR-008 scenarios](../scenarios/functional/FR-008.md) | — | Planned | — |
| FR-008-SC02 | FR-008 | Future | [#11](https://github.com/infoconex/RecordSift/issues/11) | [FR-008 scenarios](../scenarios/functional/FR-008.md) | — | Planned | — |
| FR-009-SC01 | FR-009 | Must | [#15](https://github.com/infoconex/RecordSift/issues/15) | [FR-009 scenarios](../scenarios/functional/FR-009.md) | `RecordSift.Domain.Tests.ImportSchemaTests.FR_009_SC01_RequiredColumnsBindToSourcePositionsRegardlessOfOrder` | Planned | [NUnit 48/48, Windows #41071075426](https://github.com/infoconex/RecordSift/actions/runs/41071075426) — in-memory schema/header binding unit; CSV/XLSX reader acceptance pending. |
| FR-009-SC02 | FR-009 | Must | [#15](https://github.com/infoconex/RecordSift/issues/15) | [FR-009 scenarios](../scenarios/functional/FR-009.md) | `RecordSift.Domain.Tests.ImportSchemaTests.FR_009_SC02_MissingRequiredSourceColumnReturnsSourceDiagnostic` | Planned | [NUnit 48/48, Windows #41071075426](https://github.com/infoconex/RecordSift/actions/runs/41071075426) — in-memory schema/header binding unit; CSV/XLSX reader acceptance pending. |
| FR-010-SC01 | FR-010 | Must | [#19](https://github.com/infoconex/RecordSift/issues/19) | [FR-010 scenarios](../scenarios/functional/FR-010.md) | — | Planned | — |
| FR-010-SC02 | FR-010 | Must | [#19](https://github.com/infoconex/RecordSift/issues/19) | [FR-010 scenarios](../scenarios/functional/FR-010.md) | — | Planned | — |
| FR-011-SC01 | FR-011 | Must | [#4](https://github.com/infoconex/RecordSift/issues/4) | [FR-011 scenarios](../scenarios/functional/FR-011.md) | — | Planned | — |
| FR-011-SC02 | FR-011 | Must | [#4](https://github.com/infoconex/RecordSift/issues/4) | [FR-011 scenarios](../scenarios/functional/FR-011.md) | — | Planned | — |
| FR-012-SC01 | FR-012 | Must | [#8](https://github.com/infoconex/RecordSift/issues/8) | [FR-012 scenarios](../scenarios/functional/FR-012.md) | — | Planned | — |
| FR-012-SC02 | FR-012 | Must | [#8](https://github.com/infoconex/RecordSift/issues/8) | [FR-012 scenarios](../scenarios/functional/FR-012.md) | — | Planned | — |
| FR-013-SC01 | FR-013 | Must | [#12](https://github.com/infoconex/RecordSift/issues/12) | [FR-013 scenarios](../scenarios/functional/FR-013.md) | `RecordSift.Domain.Tests.PositionalBindingTests.FR_013_SC01_NamedAndPositionalBindingsIdentifyEquivalentFields` | Planned | [NUnit 61/61, Windows #41072729348](https://github.com/infoconex/RecordSift/actions/runs/41072729348) — unit-level binding only; typed CSV/XLSX reader acceptance pending. |
| FR-013-SC02 | FR-013 | Must | [#12](https://github.com/infoconex/RecordSift/issues/12) | [FR-013 scenarios](../scenarios/functional/FR-013.md) | `RecordSift.Domain.Tests.PositionalBindingTests.FR_013_SC02_OutOfRangePositionIsAClassifiedSourceError` | Planned | [NUnit 61/61, Windows #41072729348](https://github.com/infoconex/RecordSift/actions/runs/41072729348) — unit-level binding only; typed CSV/XLSX reader acceptance pending. |
| FR-014-SC01 | FR-014 | Should | [#16](https://github.com/infoconex/RecordSift/issues/16) | [FR-014 scenarios](../scenarios/functional/FR-014.md) | — | Planned | — |
| FR-014-SC02 | FR-014 | Should | [#16](https://github.com/infoconex/RecordSift/issues/16) | [FR-014 scenarios](../scenarios/functional/FR-014.md) | — | Planned | — |
| FR-015-SC01 | FR-015 | Should | [#20](https://github.com/infoconex/RecordSift/issues/20) | [FR-015 scenarios](../scenarios/functional/FR-015.md) | — | Planned | — |
| FR-015-SC02 | FR-015 | Should | [#20](https://github.com/infoconex/RecordSift/issues/20) | [FR-015 scenarios](../scenarios/functional/FR-015.md) | — | Planned | — |
| FR-016-SC01 | FR-016 | Should | [#5](https://github.com/infoconex/RecordSift/issues/5) | [FR-016 scenarios](../scenarios/functional/FR-016.md) | — | Planned | — |
| FR-016-SC02 | FR-016 | Should | [#5](https://github.com/infoconex/RecordSift/issues/5) | [FR-016 scenarios](../scenarios/functional/FR-016.md) | — | Planned | — |
| FR-017-SC01 | FR-017 | Must | [#9](https://github.com/infoconex/RecordSift/issues/9) | [FR-017 scenarios](../scenarios/functional/FR-017.md) | — | Planned | — |
| FR-017-SC02 | FR-017 | Must | [#9](https://github.com/infoconex/RecordSift/issues/9) | [FR-017 scenarios](../scenarios/functional/FR-017.md) | — | Planned | — |
| FR-018-SC01 | FR-018 | Must | [#13](https://github.com/infoconex/RecordSift/issues/13) | [FR-018 scenarios](../scenarios/functional/FR-018.md) | — | Planned | — |
| FR-018-SC02 | FR-018 | Must | [#13](https://github.com/infoconex/RecordSift/issues/13) | [FR-018 scenarios](../scenarios/functional/FR-018.md) | — | Planned | — |
| FR-019-SC01 | FR-019 | Must | [#17](https://github.com/infoconex/RecordSift/issues/17) | [FR-019 scenarios](../scenarios/functional/FR-019.md) | — | Planned | — |
| FR-019-SC02 | FR-019 | Must | [#17](https://github.com/infoconex/RecordSift/issues/17) | [FR-019 scenarios](../scenarios/functional/FR-019.md) | — | Planned | — |
| FR-020-SC01 | FR-020 | Must | [#21](https://github.com/infoconex/RecordSift/issues/21) | [FR-020 scenarios](../scenarios/functional/FR-020.md) | — | Planned | — |
| FR-020-SC02 | FR-020 | Must | [#21](https://github.com/infoconex/RecordSift/issues/21) | [FR-020 scenarios](../scenarios/functional/FR-020.md) | — | Planned | — |
| FR-021-SC01 | FR-021 | Must | [#22](https://github.com/infoconex/RecordSift/issues/22) | [FR-021 scenarios](../scenarios/functional/FR-021.md) | — | Planned | — |
| FR-021-SC02 | FR-021 | Must | [#22](https://github.com/infoconex/RecordSift/issues/22) | [FR-021 scenarios](../scenarios/functional/FR-021.md) | — | Planned | — |
| FR-022-SC01 | FR-022 | Must | [#23](https://github.com/infoconex/RecordSift/issues/23) | [FR-022 scenarios](../scenarios/functional/FR-022.md) | — | Planned | — |
| FR-022-SC02 | FR-022 | Must | [#23](https://github.com/infoconex/RecordSift/issues/23) | [FR-022 scenarios](../scenarios/functional/FR-022.md) | — | Planned | — |
| FR-023-SC01 | FR-023 | Should | [#24](https://github.com/infoconex/RecordSift/issues/24) | [FR-023 scenarios](../scenarios/functional/FR-023.md) | — | Planned | — |
| FR-023-SC02 | FR-023 | Should | [#24](https://github.com/infoconex/RecordSift/issues/24) | [FR-023 scenarios](../scenarios/functional/FR-023.md) | — | Planned | — |
| FR-024-SC01 | FR-024 | Should | [#25](https://github.com/infoconex/RecordSift/issues/25) | [FR-024 scenarios](../scenarios/functional/FR-024.md) | — | Planned | — |
| FR-024-SC02 | FR-024 | Should | [#25](https://github.com/infoconex/RecordSift/issues/25) | [FR-024 scenarios](../scenarios/functional/FR-024.md) | — | Planned | — |
| FR-025-SC01 | FR-025 | Future | [#26](https://github.com/infoconex/RecordSift/issues/26) | [FR-025 scenarios](../scenarios/functional/FR-025.md) | — | Planned | — |
| FR-025-SC02 | FR-025 | Future | [#26](https://github.com/infoconex/RecordSift/issues/26) | [FR-025 scenarios](../scenarios/functional/FR-025.md) | — | Planned | — |
| FR-026-SC01 | FR-026 | Must | [#27](https://github.com/infoconex/RecordSift/issues/27) | [FR-026 scenarios](../scenarios/functional/FR-026.md) | — | Planned | — |
| FR-026-SC02 | FR-026 | Must | [#27](https://github.com/infoconex/RecordSift/issues/27) | [FR-026 scenarios](../scenarios/functional/FR-026.md) | — | Planned | — |
| FR-027-SC01 | FR-027 | Must | [#28](https://github.com/infoconex/RecordSift/issues/28) | [FR-027 scenarios](../scenarios/functional/FR-027.md) | — | Planned | — |
| FR-027-SC02 | FR-027 | Must | [#28](https://github.com/infoconex/RecordSift/issues/28) | [FR-027 scenarios](../scenarios/functional/FR-027.md) | — | Planned | — |
| FR-028-SC01 | FR-028 | Must | [#29](https://github.com/infoconex/RecordSift/issues/29) | [FR-028 scenarios](../scenarios/functional/FR-028.md) | — | Planned | — |
| FR-028-SC02 | FR-028 | Must | [#29](https://github.com/infoconex/RecordSift/issues/29) | [FR-028 scenarios](../scenarios/functional/FR-028.md) | — | Planned | — |
| FR-028-SC03 | FR-028 | Must | [#29](https://github.com/infoconex/RecordSift/issues/29) | [FR-028 scenarios](../scenarios/functional/FR-028.md) | — | Planned | — |
| FR-029-SC01 | FR-029 | Must | [#30](https://github.com/infoconex/RecordSift/issues/30) | [FR-029 scenarios](../scenarios/functional/FR-029.md) | — | Planned | — |
| FR-029-SC02 | FR-029 | Must | [#30](https://github.com/infoconex/RecordSift/issues/30) | [FR-029 scenarios](../scenarios/functional/FR-029.md) | — | Planned | — |
| FR-030-SC01 | FR-030 | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) | [FR-030 scenarios](../scenarios/functional/FR-030.md) | `RecordSift.Domain.Tests.ImportSummaryTests.FR_030_SC01_CompletedSummaryCountsDistinctRejectedRows` | Automated | [NUnit 16/16, Windows #41021017738](https://github.com/infoconex/RecordSift/actions/runs/41021017738) — in-memory summary unit; CSV integration pending |
| FR-030-SC02 | FR-030 | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) | [FR-030 scenarios](../scenarios/functional/FR-030.md) | `RecordSift.Domain.Tests.ImportSummaryTests.FR_030_SC02_OptionalBlankFieldWithoutErrorsCountsAsAccepted` | Automated | [NUnit 16/16, Windows #41021017738](https://github.com/infoconex/RecordSift/actions/runs/41021017738) — in-memory summary unit; CSV integration pending |
| FR-030-SC03 | FR-030 | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) | [FR-030 scenarios](../scenarios/functional/FR-030.md) | `RecordSift.Domain.Tests.ImportSummaryTests.FR_030_SC03_CompletedEmptyAndMissingHeaderFailureDifferByStatus` | Automated | [NUnit 16/16, Windows #41021017738](https://github.com/infoconex/RecordSift/actions/runs/41021017738) — in-memory summary unit; CSV integration pending |
| FR-030-SC04 | FR-030 | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) | [FR-030 scenarios](../scenarios/functional/FR-030.md) | `RecordSift.Domain.Tests.ImportSummaryTests.FR_030_SC04_MultipleErrorsPerRowDoNotInflateRejectedCount` | Automated | [NUnit 16/16, Windows #41021017738](https://github.com/infoconex/RecordSift/actions/runs/41021017738) — in-memory summary unit; CSV integration pending |
| FR-030-SC05 | FR-030 | Must | [#31](https://github.com/infoconex/RecordSift/issues/31) | [FR-030 scenarios](../scenarios/functional/FR-030.md) | `RecordSift.Domain.Tests.ImportSummaryTests.FR_030_SC05_RejectedRowsStillCompleteImportProcessing` | Automated | [NUnit 16/16, Windows #41021017738](https://github.com/infoconex/RecordSift/actions/runs/41021017738) — in-memory summary unit; CSV integration pending |
| FR-031-SC01 | FR-031 | Should | [#32](https://github.com/infoconex/RecordSift/issues/32) | [FR-031 scenarios](../scenarios/functional/FR-031.md) | — | Planned | — |
| FR-031-SC02 | FR-031 | Should | [#32](https://github.com/infoconex/RecordSift/issues/32) | [FR-031 scenarios](../scenarios/functional/FR-031.md) | — | Planned | — |
| FR-032-SC01 | FR-032 | Should | [#33](https://github.com/infoconex/RecordSift/issues/33) | [FR-032 scenarios](../scenarios/functional/FR-032.md) | — | Planned | — |
| FR-032-SC02 | FR-032 | Should | [#33](https://github.com/infoconex/RecordSift/issues/33) | [FR-032 scenarios](../scenarios/functional/FR-032.md) | — | Planned | — |
| FR-033-SC01 | FR-033 | Future | [#34](https://github.com/infoconex/RecordSift/issues/34) | [FR-033 scenarios](../scenarios/functional/FR-033.md) | — | Planned | — |
| FR-033-SC02 | FR-033 | Future | [#34](https://github.com/infoconex/RecordSift/issues/34) | [FR-033 scenarios](../scenarios/functional/FR-033.md) | — | Planned | — |
| FR-034-SC01 | FR-034 | Future | [#35](https://github.com/infoconex/RecordSift/issues/35) | [FR-034 scenarios](../scenarios/functional/FR-034.md) | — | Planned | — |
| FR-034-SC02 | FR-034 | Future | [#35](https://github.com/infoconex/RecordSift/issues/35) | [FR-034 scenarios](../scenarios/functional/FR-034.md) | — | Planned | — |
| FR-035-SC01 | FR-035 | Must | [#36](https://github.com/infoconex/RecordSift/issues/36) | [FR-035 scenarios](../scenarios/functional/FR-035.md) | — | Planned | — |
| FR-035-SC02 | FR-035 | Must | [#36](https://github.com/infoconex/RecordSift/issues/36) | [FR-035 scenarios](../scenarios/functional/FR-035.md) | — | Planned | — |
| FR-035-SC03 | FR-035 | Must | [#36](https://github.com/infoconex/RecordSift/issues/36) | [FR-035 scenarios](../scenarios/functional/FR-035.md) | — | Planned | — |
| FR-035-SC04 | FR-035 | Must | [#36](https://github.com/infoconex/RecordSift/issues/36) | [FR-035 scenarios](../scenarios/functional/FR-035.md) | — | Planned | — |
| FR-036-SC01 | FR-036 | Must | [#37](https://github.com/infoconex/RecordSift/issues/37) | [FR-036 scenarios](../scenarios/functional/FR-036.md) | — | Planned | — |
| FR-036-SC02 | FR-036 | Must | [#37](https://github.com/infoconex/RecordSift/issues/37) | [FR-036 scenarios](../scenarios/functional/FR-036.md) | — | Planned | — |
| FR-037-SC01 | FR-037 | Must | [#38](https://github.com/infoconex/RecordSift/issues/38) | [FR-037 scenarios](../scenarios/functional/FR-037.md) | — | Planned | — |
| FR-037-SC02 | FR-037 | Must | [#38](https://github.com/infoconex/RecordSift/issues/38) | [FR-037 scenarios](../scenarios/functional/FR-037.md) | — | Planned | — |
| FR-037-SC03 | FR-037 | Must | [#38](https://github.com/infoconex/RecordSift/issues/38) | [FR-037 scenarios](../scenarios/functional/FR-037.md) | — | Planned | — |
| FR-038-SC01 | FR-038 | Should | [#39](https://github.com/infoconex/RecordSift/issues/39) | [FR-038 scenarios](../scenarios/functional/FR-038.md) | — | Planned | — |
| FR-038-SC02 | FR-038 | Should | [#39](https://github.com/infoconex/RecordSift/issues/39) | [FR-038 scenarios](../scenarios/functional/FR-038.md) | — | Planned | — |
| FR-039-SC01 | FR-039 | Future | [#40](https://github.com/infoconex/RecordSift/issues/40) | [FR-039 scenarios](../scenarios/functional/FR-039.md) | — | Planned | — |
| FR-039-SC02 | FR-039 | Future | [#40](https://github.com/infoconex/RecordSift/issues/40) | [FR-039 scenarios](../scenarios/functional/FR-039.md) | — | Planned | — |
| NFR-001-SC01 | NFR-001 | Must | [#41](https://github.com/infoconex/RecordSift/issues/41) | [NFR-001 scenarios](../scenarios/non-functional/NFR-001.md) | — | Planned | — |
| NFR-001-SC02 | NFR-001 | Must | [#41](https://github.com/infoconex/RecordSift/issues/41) | [NFR-001 scenarios](../scenarios/non-functional/NFR-001.md) | — | Planned | — |
| NFR-002-SC01 | NFR-002 | Must | [#42](https://github.com/infoconex/RecordSift/issues/42) | [NFR-002 scenarios](../scenarios/non-functional/NFR-002.md) | — | Planned | — |
| NFR-002-SC02 | NFR-002 | Must | [#42](https://github.com/infoconex/RecordSift/issues/42) | [NFR-002 scenarios](../scenarios/non-functional/NFR-002.md) | — | Planned | — |
| NFR-003-SC01 | NFR-003 | Must | [#43](https://github.com/infoconex/RecordSift/issues/43) | [NFR-003 scenarios](../scenarios/non-functional/NFR-003.md) | — | Planned | — |
| NFR-003-SC02 | NFR-003 | Must | [#43](https://github.com/infoconex/RecordSift/issues/43) | [NFR-003 scenarios](../scenarios/non-functional/NFR-003.md) | — | Planned | — |
| NFR-004-SC01 | NFR-004 | Must | [#44](https://github.com/infoconex/RecordSift/issues/44) | [NFR-004 scenarios](../scenarios/non-functional/NFR-004.md) | — | Planned | — |
| NFR-004-SC02 | NFR-004 | Must | [#44](https://github.com/infoconex/RecordSift/issues/44) | [NFR-004 scenarios](../scenarios/non-functional/NFR-004.md) | — | Planned | — |
| NFR-005-SC01 | NFR-005 | Must | [#45](https://github.com/infoconex/RecordSift/issues/45) | [NFR-005 scenarios](../scenarios/non-functional/NFR-005.md) | — | Planned | — |
| NFR-005-SC02 | NFR-005 | Must | [#45](https://github.com/infoconex/RecordSift/issues/45) | [NFR-005 scenarios](../scenarios/non-functional/NFR-005.md) | — | Planned | — |
| NFR-006-SC01 | NFR-006 | Must | [#46](https://github.com/infoconex/RecordSift/issues/46) | [NFR-006 scenarios](../scenarios/non-functional/NFR-006.md) | — | Planned | — |
| NFR-006-SC02 | NFR-006 | Must | [#46](https://github.com/infoconex/RecordSift/issues/46) | [NFR-006 scenarios](../scenarios/non-functional/NFR-006.md) | — | Planned | — |
| NFR-007-SC01 | NFR-007 | Must | [#47](https://github.com/infoconex/RecordSift/issues/47) | [NFR-007 scenarios](../scenarios/non-functional/NFR-007.md) | — | Planned | — |
| NFR-007-SC02 | NFR-007 | Must | [#47](https://github.com/infoconex/RecordSift/issues/47) | [NFR-007 scenarios](../scenarios/non-functional/NFR-007.md) | — | Planned | — |
| NFR-008-SC01 | NFR-008 | Must | [#48](https://github.com/infoconex/RecordSift/issues/48) | [NFR-008 scenarios](../scenarios/non-functional/NFR-008.md) | — | Planned | — |
| NFR-008-SC02 | NFR-008 | Must | [#48](https://github.com/infoconex/RecordSift/issues/48) | [NFR-008 scenarios](../scenarios/non-functional/NFR-008.md) | — | Planned | — |
| NFR-009-SC01 | NFR-009 | Must | [#49](https://github.com/infoconex/RecordSift/issues/49) | [NFR-009 scenarios](../scenarios/non-functional/NFR-009.md) | — | Planned | — |
| NFR-009-SC02 | NFR-009 | Must | [#49](https://github.com/infoconex/RecordSift/issues/49) | [NFR-009 scenarios](../scenarios/non-functional/NFR-009.md) | — | Planned | — |
| NFR-010-SC01 | NFR-010 | Must | [#50](https://github.com/infoconex/RecordSift/issues/50) | [NFR-010 scenarios](../scenarios/non-functional/NFR-010.md) | — | Planned | — |
| NFR-010-SC02 | NFR-010 | Must | [#50](https://github.com/infoconex/RecordSift/issues/50) | [NFR-010 scenarios](../scenarios/non-functional/NFR-010.md) | — | Planned | — |

## Release 0.1 exit conditions

Every in-scope Must scenario has an automated passing test or approved, reproducible manual evidence. The executable test/check column and evidence column are populated in the implementation PR, and the declared framework/toolchain is verified before release. Scenarios for deferred target frameworks are explicitly recorded as deferred even when they share a requirement with release 0.1.
