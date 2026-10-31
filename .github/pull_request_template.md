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

**Merge policy:** squash merge issue PRs into `release/0.1`. The separately authorized release-promotion PR must also **SQUASH merge into `main`** (one consolidated release commit), after green Windows full test/security/API, independent NuGet consumer (RC and stable) and documentation PR checks. Reverify those gates on the exact resulting `main` SHA. **No release tag, GitHub release or public package publication** without a distinct owner authorization.
