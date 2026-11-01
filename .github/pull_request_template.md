## Primary issue

Closes #<!-- issue number -->

**Requirement:** `FR-###` / `NFR-###` / engineering issue  
**Scenario(s):** `FR-###-SC01`

## Scope and intended behavior

Describe the smallest change that satisfies this single issue. Cross-reference related issues rather than implementing them here.

## Test evidence

- [ ] Red test created first (or explain why the change is documentation-only).
- [ ] Relevant positive, negative, and boundary tests added.
- [ ] Commands, framework and SDK versions documented.
- [ ] CI run or observed test output linked; otherwise explicitly note unverified.
- [ ] Traceability matrix updated (or explain documentation-only exception).

**Commands and observed results:**

<!-- Exact commands, pass/fail counts, and workflow links. Do not invent evidence. -->

## Review and risk

- [ ] PR base is `release/0.1` for an issue branch (or `main` for the approved release PR).
- [ ] No unrelated functionality or framework/toolchain upgrades.
- [ ] Dependency compatibility, licenses and advisories reviewed if packages changed.
- [ ] Public API and report format compatibility reviewed.
- [ ] Docs, specs and acceptance criteria updated if the contract changed.
- [ ] Security/privacy implications of untrusted files considered.
- [ ] Infrastructure projects use `RecordSift.Infrastructure.<Format>`; concrete implementations that bridge third-party APIs use `Adapter` suffix, while application interfaces remain vendor-neutral.
- [ ] Public Domain/Application C# namespaces reflect their physical layer and responsibility; do not introduce `namespace RecordSift` flat type declarations. For breaking changes update ADR 0015 migration documentation and API tests.
- [ ] Physical assembly dependency graph is `Domain <- Application <- Infrastructure` (vendor adapter may also directly reference Domain), checked by `scripts/validate_architecture_layers.py` and project references; public API assembly moves reviewed.
- [ ] Replaceable input/validation seams have fake-based unit tests **and** real-adapter component tests when adapters change (or explain why not applicable).
- [ ] Domain-owned diagnostic codes, invariant resource-backed messages and configuration defaults preserved; no hardcoded production prose or vendor exception text leaks.
- [ ] New argument messages use named Domain-owned `DomainMessageResources` accessors; no public `Get(string)` or raw `Argument_*` keys outside catalog (ADR 0016).
- [ ] Core unit tests do not require parser vendors; adapter-specific tests independently prove the same RecordSift-owned behavioral contract (separate NUnit assemblies enforced under #91).
- [ ] Microsoft C# naming is enforced by Roslyn IDE1006 and constant identifier guard in CI; do not use uppercase constant names even when external diagnostic string values are uppercase.
- [ ] SOLID responsibilities, Microsoft C# naming/style (`.editorconfig`) and net48/C# 7.3 language compatibility reviewed.
- [ ] Any third-party dependency is confined to an adapter/test assembly; exact version, license, framework compatibility, audit/CI inventory and ADR reviewed.

## Additional gates — only for a release-promotion PR targeting `main`

See [release promotion and publication governance](../blob/release/0.1/docs/engineering/release-gates.md) and the [current 0.1 prepublication audit](../blob/release/0.1/docs/engineering/release-0.1-prepublication-audit.md). If the links above do not resolve in your PR view, use `docs/engineering/release-gates.md` and `docs/engineering/release-0.1-prepublication-audit.md` directly.

- [ ] The exact integration source HEAD SHA and target `main` base SHA are recorded, and the candidate is frozen.
- [ ] All release-scoped requirements and deferrals are accounted for; blocker issues and residual risks are resolved or explicitly accepted.
- [ ] An independent human has reviewed the complete production code change, API/compatibility, source security/error paths and current documentation, and GitHub records their approval.
- [ ] All **four job contexts** (Windows NUnit/API, dependency audit, installed consumer, docs) are required by an active `main` ruleset and green on the final PR SHA.
- [ ] The existing release candidate was validated through separately installed RC and stable local NuGet consumers with package metadata, license and dependency evidence.
- [ ] The release owner explicitly granted **source-promotion GO for this candidate** in the PR; neither assistant assessment nor CI success substitutes for approval.
- [ ] The PR uses a **SQUASH merge**. On the resulting unique `main` commit, reverify all jobs and document their links.
- [ ] Any public tagging, GitHub release, package feed upload or distribution has separate explicit release-owner authorization, outside this PR.

**Merge policy:** Issue PRs squash-merge into `release/0.1`. Release-promotion PRs squash-merge into `main` only after the independent reviewer, owner GO, active protection and mandatory CI gates in `docs/engineering/release-gates.md` are actually satisfied. If a required gate is unverified, **STOP**; do not auto-merge. A source merge never grants public distribution permission.
