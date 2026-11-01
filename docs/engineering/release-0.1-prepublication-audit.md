# Release 0.1 — preliminary prepublication audit and unresolved evidence

**Assessment date:** 2026-10-31. **Status: INCOMPLETE / NO-GO for public publication.** This is a **targeted assistant-led technical review**, **not an independent human code/security review** and not a certification that the entire source repository is free of defects. It does not replace the comprehensive tracked-document inventory review in [the prior documentation audit](release-0.1-documentation-audit.md).

**Audited source state:** `main` commit `26facaff671599d20ff6e8a1745d0d01450b0757`; identical source tree at `release/0.1` commit `ccb010360f7de9aa98f52347ad7de7219b253f75` prior to the new governance issue branch. Source promotion was [PR #198](https://github.com/infoconex/RecordSift/pull/198), squash-only. Do not confuse later governance documentation commits with the binary candidate whose CI passed.

## Verified automated evidence for the existing source candidate

- [Windows .NET Framework 4.8 build and test, run 41244006287](https://github.com/infoconex/RecordSift/actions/runs/41244006287): successful; 379 NUnit cases across five test assemblies, 425-entry public API baseline, seven JSON compatibility fixtures, XML doc checks and architecture/style validations.
- [Isolated Windows NuGet consumer, run 41244006279](https://github.com/infoconex/RecordSift/actions/runs/41244006279): successful; independently installed unpublished RC and locally built stable `0.1.0` package candidates in separate feeds. This is **not** a test of a published package.
- [Documentation CI, run 41244006317](https://github.com/infoconex/RecordSift/actions/runs/41244006317): successful for traceability, internal links and engineering validation.
- [Prior 127-file documentation audit](release-0.1-documentation-audit.md) records inventory review but expressly does **not** guarantee all factual or semantic defects were found.
- GitHub confirms no open pull requests or GitHub releases at this candidate checkpoint. Six Future functional issues are deferred and not 0.1 release blockers by approved scope.

## Targeted source/security/consumer inspection completed

The reviewer fetched and inspected the production CSV reader, stream importer, character-budget reader, import orchestration and shared validation, XLSX reader/importer, structured diagnostics/results, JSON and diagnostic CSV serializers, safe identifier handling, reporting acceptance tests, `RecordSift.nuspec`, Windows and docs/consumer workflow definitions, current consumer README and release documentation. Checks included ownership/disposal, parser exception boundaries, eager parsing and cancellation, CSV/XLSX resource controls, JSON safe-message handling, dependency layout and packaging promises. Inspection of individual source files is **not** equivalent to running adversarial/fuzz tests or auditing every code path. The CI validation is stronger than a static reading for tested scenarios but does not prove untested behavior.

## Findings and release decisions

| ID | Classification | Finding | Disposition |
| --- | --- | --- | --- |
| GOV-01 | **Release gate** | `main` reports `protected: false` and no active rulesets; passing CI is not mechanically required before promotion. | **OPEN** [#199](https://github.com/infoconex/RecordSift/issues/199); admin action and enforcement proof required. |
| GOV-02 | **Release gate** | PR #198 and the documentation promotion PRs #197/#195 have no submitted independent GitHub reviews. Current green checks do not substitute for an independent reviewer. | **OPEN** [#199](https://github.com/infoconex/RecordSift/issues/199); a named reviewer must evaluate source and record a real approval or a documented blocking finding. Do not backdate an approval for PR #198. |
| SEC-01 | **Security/compatibility decision** | Diagnostic CSV preserves untrusted `SourceId`, field and worksheet metadata through RFC-style quoting, but quoting does not prevent spreadsheet clients from interpreting formula-like cell values. The consumer guide and release notes **already warn** about this: the residual risk is documented, but no spreadsheet execution experiment or explicit public-risk acceptance is recorded. | **OPEN** [#200](https://github.com/infoconex/RecordSift/issues/200). Decide safe-for-spreadsheets versus machine-readable-only CSV, add the right tests and record release-owner acceptance before publication. Do not silently break metadata fidelity. |
| PACK-01 | **Prepublication gate** | Source `packaging/RecordSift.nuspec` defaults to `0.1.0-rc.1` and names `release/0.1` as the repository branch. The stable local candidate is built by a checked version override. The correct public stable identity, repository ref and commit association have **not** been finalized as a published artifact. | Verify or correct final package metadata, source commit and checksum on the exact published build; new PR and review if source changes are necessary. |
| SEC-02 | **Evidence gap** | Dependency NuGet advisory checks passed and permissions are read-only in workflows; no comprehensive repository secret scanner result, third-party penetration test or fuzzing certification was verified in this review. | Independent reviewer to scope/perform or explicitly risk-accept appropriate additional checks. Do not describe the current automated results as complete penetration/security testing. |
| QA-01 | **Evidence gap** | Existing NUnit/contract/consumer tests cover accepted Windows/net48 scenarios; no fresh independent human source-wide code review or exhaustive adversarial CSV/XLSX testing has been documented for this assessment. | Named independent reviewer to inspect release diff/source and validate error/resource/security risks, recording approval or corrective issues. |
| QA-02 | **Known limitation** | Both importers eagerly parse bounded source data; cancellation does not interrupt all reads and memory use is bounded by configured limits rather than streaming. | **Accepted documented scope** (Future FR-034/FR-033); do not misrepresent as streaming/async support. |
| QA-03 | **Future/publication** | The package's locally built stable checksum is an ephemeral CI result; byte-identical cross-machine rebuilds are not established. | Generate and retain final checksum and independent published-consumer verification only after separately approved distribution. |

**Technical observation, not a reproduced exploit:** [CsvImportResultSerializerAdapter](../../src/RecordSift.Infrastructure.Reporting/CsvImportResultSerializerAdapter.cs) serializes caller-controlled `SourceId` and diagnostic metadata. `AppendField` quotes delimiter/quote/CR/LF but does not neutralize initial spreadsheet formula characters. This is semantically valid *interchange CSV*; the user-facing risk depends on spreadsheet evaluation, which is outside the current serializer's stated contract. Do not assert Excel execution without testing it.

## Minimum remaining work before a publication GO

1. GitHub administrator applies and verifies `main` ruleset, mandatory four job checks, independent PR review and no direct pushes; evidence in [#199](https://github.com/infoconex/RecordSift/issues/199).
2. Independent human reviews production source, security implications and current documentation; records findings and signed decision against an exact commit and full release delta. Do not close based solely on assistant inspection.
3. Decide/mitigate or explicitly accept [#200](https://github.com/infoconex/RecordSift/issues/200), then resolve any code/documentation blockers with new reviewed issue PRs.
4. Refreeze the exact release candidate if changes occurred; rerun Windows, isolated RC/stable consumer, documentation and dependency checks on the **new SHA**, and validate stable packaging metadata and distribution checksum.
5. Record owner GO separately for **any further source promotion** and later for **public publication**. No tag, public GitHub Release or NuGet upload while this document says NO-GO.

The existing `main` source commit is not automatically defective because governance was weak. No automatic rollback or rewriting of `main` is recommended absent a reproducible defect or an owner-authorized remedy.
