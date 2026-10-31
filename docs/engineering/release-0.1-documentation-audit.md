# Release 0.1 — Complete tracked-document review and promotion gate (issue #196)

**Inventory snapshot:** `a3a056366cd05a2ed1a28cbff27ebfab90e13723` (`release/0.1` before issue #196 documentation corrections); **date:** 2026-10-31; **scope:** **127 of 127 tracked documentation surfaces** (114 Markdown files and 13 ancillary normative documentation/configuration assets). This ledger is the additional 128th document created to record the review and is itself included in the acceptance review. The inventory below names **every reviewed path**, records the inspected baseline Git blob ID, and identifies corrected files.

## Review standard and disposition

Every listed file was retrieved and reviewed for release 0.1 applicability, authoritative versus historical status, requirement/acceptance meaning, Windows/.NET Framework 4.8/C# 7.3 compatibility, license/distribution boundaries, consistency with the accepted software and tests, relative links and needed corrections. Focused deeper semantic reads were performed for current-facing/flagged specifications, test strategy, guides and release procedures. The pre-existing CI documentation validation remains mandatory for Markdown link integrity, requirement/scenario counts, naming, architecture and test metadata; independent Windows build/API and installed-consumer gates must also pass. **File coverage is 100%; this is not a claim that every possible factual, legal or external-link defect is impossible.**

**Release evidence:** 25 Must + 8 targeted Should functional requirements: **77 Automated functional scenarios**; 6 Future requirements: **12 Planned/deferred functional scenarios**. Ten scoped NFRs: **18 Automated** and **one Manual** with reproducible review. **108 active scenario rows** (89 FR + 19 NFR); one additional historical NFR-009-SC02 ID is retired, not an active acceptance gate. Baseline Windows CI passed **379/379 NUnit**, **425 approved API/metadata entries**, seven JSON golden fixtures, five test assemblies, NuGet pinned-package audit and independently installed RC+stable 0.1.0 consumers. Historical source documents remain intact rather than incorrectly rewritten as if their earlier intermediate status were current.

**Status codes:** **Updated** = substantive release-accuracy correction made on this issue branch; **Reviewed** = checked, no material change needed; **Historical** = prior decision retained with current implementation precedence documented; **Deferred** = intentionally excluded Future functionality and correctly specified as such; **Validated artifact** = versioned machine-readable baseline, license or CI configuration reviewed and cross-checked by the applicable automated gate. For exact changes and fresh results, follow [issue #196](https://github.com/infoconex/RecordSift/issues/196) and its PR.

## Findings and corrective actions

1. The main [engineering index](README.md) incorrectly described implemented mapping, validation, CSV/XLSX integration, JSON fixtures and final acceptance as future work; reconciled with the release evidence, clarified historical ADRs versus current authority, and documented the separate squash-only source promotion/publication gates.
2. Fourteen completed Must-priority FR specifications retained obsolete “tests not implemented” or “acceptance outstanding” wording. Preserved required `Planned` **specification** metadata while explicitly pointing to observed Automated **execution** evidence. Deferred Future specs remain deliberately Planned.
3. The [record-import specification](../specifications/record-import.md) still claimed unresolved XLSX/generalized validation, unsupported diagnostic severity and `WarningRowCount`, proposed JSON contracts and open design choices. Reconciled with the error-only implementation and accepted release semantics.
4. The [testing strategy](../testing/strategy.md) referred to three rather than five NUnit suites, proposed (rather than pinned) NUnit, and a nonexistent `RecordSift.ContractTests` path; corrected to the real Reporting fixture directory and observed net48 CI.
5. The [JSON report specification](../specifications/import-result.md) claimed the diagnostic registry and executable semantic contract tests were not yet implemented; corrected without pretending to execute a full Draft 2020-12 schema engine.
6. The [legacy baseline guide](baseline.md) contained runnable commands pointing to removed projects. Replaced executable instructions with current solution/test workflow; retained original issue #57 project inventory **explicitly as historical evidence**.
7. The functional catalog, scenario index and traceability overview now distinguish **109 historical scenario IDs** from **108 active rows**, with 95 Automated, one Manual and 12 planned/deferred. API baseline docs distinguish initial 375 entries from current 425 entries.
8. The README/PR workflow, release checklist, readiness report, dependencies and release notes distinguish owner-approved **source-only SQUASH promotion** from unauthorized public tags, GitHub releases and NuGet uploads. ADR corrections annotate superseded intermediate states rather than rewriting accepted history.
9. Supporting metadata was inspected: MIT LICENSE and 11 dependency notices, pinned `.nuspec` dependencies, Draft 2020-12 schema variants 1.0/1.1/1.2, 425-entry compiled API text baseline, documentation/Windows/consumer workflow triggers for both `release/**` and `main`, code style, NuGet audit configuration and issue templates.

## File-by-file review register

Baseline files: **127 / 127 inventoried and reviewed**. `Blob` identifies the exact source revision inspected before edits; changed files receive new blob IDs in the documentation PR. The self-referential ledger is listed at the end as a new checked document.

### Core guidance, release artifacts and supporting documentation

| Tracked document at inventory SHA | Reviewed baseline blob | Disposition |
| --- | --- | --- |
| `.editorconfig` | `b67b10c3b75d` | Validated artifact / Reviewed |
| `.github/ISSUE_TEMPLATE/bug_report.yml` | `49e7d629d510` | Validated artifact / Reviewed |
| `.github/ISSUE_TEMPLATE/requirement.yml` | `29198add8732` | Validated artifact / Reviewed |
| `.github/agents/requirements-reviewer.agent.md` | `02e13ff23d7a` | Reviewed |
| `.github/agents/tdd-coach.agent.md` | `4996b95268d5` | Reviewed |
| `.github/copilot-instructions.md` | `f81f71ec339b` | Reviewed |
| `.github/pull_request_template.md` | `73c44ffe6e8d` | Updated |
| `.github/workflows/validate-docs.yml` | `7c01f92602c6` | Validated artifact / Reviewed |
| `.github/workflows/verify-rc-consumer.yml` | `53a13e7cc1be` | Validated artifact / Reviewed |
| `.github/workflows/windows-net48.yml` | `99add79e559f` | Validated artifact / Reviewed |
| `AGENTS.md` | `08b877c2e087` | Updated |
| `LICENSE` | `79d1d8e4574e` | Validated artifact / Reviewed |
| `NuGet.Config` | `4bf83d6865e0` | Validated artifact / Reviewed |
| `README.md` | `19df9bccfa9c` | Reviewed |
| `THIRD-PARTY-NOTICES.md` | `0ac5ff3537f6` | Reviewed |
| `docs/README.md` | `d538f41fd5f8` | Updated |
| `docs/compatibility/README.md` | `890f797f8cb2` | Updated |
| `docs/compatibility/public-api-net48.txt` | `0153a2b0f52f` | Validated artifact / Reviewed |
| `docs/engineering/architecture.md` | `c146c35ca273` | Reviewed |
| `docs/engineering/baseline.md` | `edc1b5ade1ee` | Updated |
| `docs/engineering/csharp-style.md` | `6d3d00b2197a` | Reviewed |
| `docs/engineering/platform-dependencies-net48.md` | `65c93b54c840` | Updated |
| `docs/engineering/release-0.1-functional-completion.md` | `6153822ea6ca` | Updated |
| `docs/engineering/release-0.1-packaging.md` | `a67ceb9329b4` | Reviewed |
| `docs/engineering/release-0.1-readiness.md` | `ccf990728da4` | Updated |
| `docs/engineering/workflow.md` | `94f6f81f76f0` | Updated |
| `docs/release/0.1-consumer-guide.md` | `b413e4d6f9f0` | Reviewed |
| `docs/release/0.1-dependencies.md` | `0cf24335ea44` | Updated |
| `docs/release/0.1-release-checklist.md` | `8184cb6e4a43` | Reviewed |
| `docs/release/0.1-release-notes.md` | `cecdd2167c7b` | Updated |
| `docs/requirements/functional.md` | `dd8bfd159534` | Updated |
| `docs/requirements/non-functional.md` | `83c4f593f29d` | Reviewed |
| `docs/scenarios/README.md` | `08f2466bb78e` | Updated |
| `docs/specifications/import-result.md` | `2e758767dba4` | Updated |
| `docs/specifications/record-import.md` | `319b5524efc4` | Updated |
| `docs/specifications/schemas/import-result-canceled.schema.json` | `83bded7b0c90` | Validated artifact / Reviewed |
| `docs/specifications/schemas/import-result-stopped.schema.json` | `fae80bb1b8f3` | Validated artifact / Reviewed |
| `docs/specifications/schemas/import-result.schema.json` | `1f50f6ff2460` | Validated artifact / Reviewed |
| `docs/testing/release-candidate-consumer.md` | `a69153597f33` | Reviewed |
| `docs/testing/strategy.md` | `e3f6634bb1d3` | Updated |
| `docs/testing/traceability.md` | `c7eb0055df3e` | Updated |
| `packaging/README.md` | `53356b995c43` | Reviewed |
| `packaging/RecordSift.nuspec` | `1aa27bd6d8cd` | Validated artifact / Reviewed |

### Historical architecture decisions (preserved with explicit subsequent updates where needed)

| Tracked document at inventory SHA | Reviewed baseline blob | Disposition |
| --- | --- | --- |
| `docs/engineering/decisions/0001-in-memory-import-summary.md` | `08973698afba` | Historical / Reviewed |
| `docs/engineering/decisions/0002-structured-diagnostics.md` | `b194bde6bdce` | Historical / Reviewed |
| `docs/engineering/decisions/0003-source-column-binding.md` | `4509a8c83818` | Historical / Reviewed |
| `docs/engineering/decisions/0004-positional-column-binding.md` | `ba0c10035c0a` | Updated |
| `docs/engineering/decisions/0005-bounded-csv-reader.md` | `d9291a6a4932` | Updated |
| `docs/engineering/decisions/0006-csv-header-policy.md` | `9b983133d931` | Historical / Reviewed |
| `docs/engineering/decisions/0007-domain-ports-and-adapters.md` | `1a74941be27b` | Historical / Reviewed |
| `docs/engineering/decisions/0008-csvhelper-isolated-adapter.md` | `5fddacb57859` | Historical / Reviewed |
| `docs/engineering/decisions/0009-configurable-csv-delimiters.md` | `ceb119ad8500` | Historical / Reviewed |
| `docs/engineering/decisions/0010-domain-message-resources.md` | `0b68d0e37e15` | Historical / Reviewed |
| `docs/engineering/decisions/0011-independent-core-contract-tests.md` | `9f760ee10c05` | Historical / Reviewed |
| `docs/engineering/decisions/0012-microsoft-csharp-naming-ci.md` | `04f6551c2b75` | Historical / Reviewed |
| `docs/engineering/decisions/0013-physical-domain-application-infrastructure-assemblies.md` | `53ae7216274e` | Historical / Reviewed |
| `docs/engineering/decisions/0014-infrastructure-csv-adapter-naming.md` | `06e2fff78287` | Historical / Reviewed |
| `docs/engineering/decisions/0015-layer-aligned-public-namespaces.md` | `35f92bd25860` | Historical / Reviewed |
| `docs/engineering/decisions/0016-typed-domain-resource-messages.md` | `97aba799e5f6` | Historical / Reviewed |
| `docs/engineering/decisions/0017-application-ports-namespace.md` | `ddbdebbd0871` | Historical / Reviewed |
| `docs/engineering/decisions/0018-independent-domain-application-tests.md` | `0dd6a221b860` | Historical / Reviewed |
| `docs/engineering/decisions/0019-approved-core-framework-dependencies.md` | `8bb567c5f366` | Historical / Reviewed |
| `docs/engineering/decisions/0020-application-csv-message-ownership.md` | `c63fc528951f` | Historical / Reviewed |
| `docs/engineering/decisions/0021-focused-csv-infrastructure-classes.md` | `e7f011f6a018` | Historical / Reviewed |
| `docs/engineering/decisions/0022-first-typed-csv-import.md` | `a2f6d46cd80a` | Updated |
| `docs/engineering/decisions/0023-explicit-string-property-mapping.md` | `3e0ce0070842` | Updated |
| `docs/engineering/decisions/0024-explicit-primitive-conversion.md` | `7d0b0b33ddd6` | Historical / Reviewed |
| `docs/engineering/decisions/0025-configurable-csv-text-encoding.md` | `2623c4b370e1` | Historical / Reviewed |
| `docs/engineering/decisions/0026-domain-field-length-validation.md` | `399d7dd30f0e` | Historical / Reviewed |
| `docs/engineering/decisions/0027-domain-typed-range-validation.md` | `4dcbfac90a32` | Historical / Reviewed |
| `docs/engineering/decisions/0028-per-import-duplicate-identifiers.md` | `0d0fe4bf7b2f` | Historical / Reviewed |
| `docs/engineering/decisions/0029-domain-custom-record-validation.md` | `288bd1a2034e` | Historical / Reviewed |
| `docs/engineering/decisions/0030-isolated-json-import-reporting.md` | `5d5447932846` | Historical / Reviewed |
| `docs/engineering/decisions/0031-invariant-readable-import-reporting.md` | `82213c9e00e9` | Historical / Reviewed |
| `docs/engineering/decisions/0032-xlsx-reader-shared-validation.md` | `a31c9cb2f4a8` | Updated |
| `docs/engineering/decisions/0033-named-xlsx-worksheet-selection.md` | `640b683748b8` | Historical / Reviewed |
| `docs/engineering/decisions/0034-public-library-constants.md` | `7f0d313fdf7e` | Historical / Reviewed |
| `docs/engineering/decisions/0035-least-public-api-resource-boundaries.md` | `27a1bde82c72` | Historical / Reviewed |

### Functional requirement specifications (legacy Planned header is not execution status)

| Tracked document at inventory SHA | Reviewed baseline blob | Disposition |
| --- | --- | --- |
| `docs/scenarios/functional/FR-001.md` | `76ec41666327` | Reviewed |
| `docs/scenarios/functional/FR-002.md` | `dfe4a0e2b8cb` | Updated |
| `docs/scenarios/functional/FR-003.md` | `4bc46399ea2d` | Updated |
| `docs/scenarios/functional/FR-004.md` | `7f90575300c6` | Updated |
| `docs/scenarios/functional/FR-005.md` | `faad7e580898` | Updated |
| `docs/scenarios/functional/FR-006.md` | `b1293f351bf4` | Updated |
| `docs/scenarios/functional/FR-007.md` | `4a4286f24186` | Deferred / Reviewed |
| `docs/scenarios/functional/FR-008.md` | `7f8aa65af125` | Deferred / Reviewed |
| `docs/scenarios/functional/FR-009.md` | `d226c5f5c9db` | Reviewed |
| `docs/scenarios/functional/FR-010.md` | `20748294245d` | Reviewed |
| `docs/scenarios/functional/FR-011.md` | `fec45c43f0f0` | Reviewed |
| `docs/scenarios/functional/FR-012.md` | `e4169cce6dfb` | Updated |
| `docs/scenarios/functional/FR-013.md` | `8b53fe6b5e1c` | Reviewed |
| `docs/scenarios/functional/FR-014.md` | `0621ce70c058` | Reviewed |
| `docs/scenarios/functional/FR-015.md` | `a07d1b0438b5` | Reviewed |
| `docs/scenarios/functional/FR-016.md` | `2252668e1487` | Reviewed |
| `docs/scenarios/functional/FR-017.md` | `25b6a2844be7` | Reviewed |
| `docs/scenarios/functional/FR-018.md` | `2d0210a34d12` | Reviewed |
| `docs/scenarios/functional/FR-019.md` | `41bdde6711f7` | Updated |
| `docs/scenarios/functional/FR-020.md` | `d745a1ccd08a` | Updated |
| `docs/scenarios/functional/FR-021.md` | `fed8a5cf9d8e` | Updated |
| `docs/scenarios/functional/FR-022.md` | `dd65db5edc49` | Updated |
| `docs/scenarios/functional/FR-023.md` | `ac5e41c7076f` | Reviewed |
| `docs/scenarios/functional/FR-024.md` | `1719b78a47fb` | Reviewed |
| `docs/scenarios/functional/FR-025.md` | `b97a983bf363` | Deferred / Reviewed |
| `docs/scenarios/functional/FR-026.md` | `f4edaf73067e` | Reviewed |
| `docs/scenarios/functional/FR-027.md` | `a08da7ad551f` | Reviewed |
| `docs/scenarios/functional/FR-028.md` | `7c5a27f6ae7d` | Reviewed |
| `docs/scenarios/functional/FR-029.md` | `c4cb7d967926` | Reviewed |
| `docs/scenarios/functional/FR-030.md` | `1399e8542d51` | Updated |
| `docs/scenarios/functional/FR-031.md` | `f1fe05213b7b` | Reviewed |
| `docs/scenarios/functional/FR-032.md` | `4fa0ec22f6f1` | Reviewed |
| `docs/scenarios/functional/FR-033.md` | `e66426cf330c` | Deferred / Reviewed |
| `docs/scenarios/functional/FR-034.md` | `05f9853945a5` | Deferred / Reviewed |
| `docs/scenarios/functional/FR-035.md` | `31cb3bce884f` | Updated |
| `docs/scenarios/functional/FR-036.md` | `18738486567d` | Updated |
| `docs/scenarios/functional/FR-037.md` | `dc508d217c45` | Updated |
| `docs/scenarios/functional/FR-038.md` | `f284d27f770d` | Reviewed |
| `docs/scenarios/functional/FR-039.md` | `327c1de1040e` | Deferred / Reviewed |

### Nonfunctional requirement specifications (normative Maintained status)

| Tracked document at inventory SHA | Reviewed baseline blob | Disposition |
| --- | --- | --- |
| `docs/scenarios/non-functional/NFR-001.md` | `46fe1e39ded4` | Reviewed |
| `docs/scenarios/non-functional/NFR-002.md` | `356fa621db2b` | Reviewed |
| `docs/scenarios/non-functional/NFR-003.md` | `8b1a5fae2d1b` | Reviewed |
| `docs/scenarios/non-functional/NFR-004.md` | `aa61c62176d3` | Reviewed |
| `docs/scenarios/non-functional/NFR-005.md` | `8cbde91ed5ab` | Reviewed |
| `docs/scenarios/non-functional/NFR-006.md` | `aa65125a4921` | Reviewed |
| `docs/scenarios/non-functional/NFR-007.md` | `95711a6fb5ed` | Reviewed |
| `docs/scenarios/non-functional/NFR-008.md` | `9ded4755a48f` | Reviewed |
| `docs/scenarios/non-functional/NFR-009.md` | `29b80bf8544d` | Reviewed |
| `docs/scenarios/non-functional/NFR-010.md` | `ffbc379489a9` | Reviewed |

| `docs/engineering/release-0.1-documentation-audit.md` | New in #196 | Reviewed / audit ledger |

## Acceptance and remaining gates

**Required before source promotion:** review and pass CI on the documentation PR, squash it to `release/0.1`, verify three post-merge workflows on the exact integration SHA, open a dedicated `release/0.1` → `main` PR, confirm three green main-targeted PR workflows, **SQUASH** into `main` to create one consolidated commit, and rerun all three workflows on the exact `main` SHA. If any validation fails, stop and correct the source/traceability/ledger rather than treating an audit checklist as sufficient.

**Not authorized:** package feed upload, public tag, GitHub release or other distribution. A locally staged `0.1.0` package was technically validated but is not a retained/sign-off public artifact; a final release needs distinct owner approval, artifact checksums, appropriate channel, and external-install smoke tests. Future cross-platform support and the six Future FRs remain out of scope.

**Scope limitation:** external websites, source-level security guarantees, legal interpretation of vendor license obligations, and byte-identical package reproducibility are not certified by a Markdown inventory or documentation CI. Review those separately at publication time.
