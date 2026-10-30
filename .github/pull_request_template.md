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

**Merge policy:** squash merge issue PRs into `release/0.1`. Do not merge incomplete releases into `main`.
