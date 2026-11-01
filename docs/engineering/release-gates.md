# Release promotion and publication governance

**Status: active CI/PR protection; solo-maintainer sign-off policy.** Tracked by [issue #199](https://github.com/infoconex/RecordSift/issues/199). On 2026-10-31, GitHub reported `main.protected = true` and [active ruleset #26295899](https://github.com/infoconex/RecordSift/rules/26295899) on `refs/heads/main` with the four named required GitHub Actions job checks, squash-only PRs and no bypass actors. The repository has **one maintainer**; required GitHub reviewer approvals deliberately remain **0**. This does not constitute an independent review or automatically enforce the owner's release-readiness GO. [Release 0.1 preliminary audit](release-0.1-prepublication-audit.md).

## Separate decisions

1. **Issue integration:** a reviewed issue branch is squash-merged into `release/0.1` after appropriate checks. The integration branch is not a released build.
2. **Source promotion:** a frozen, reviewed release candidate is squash-merged to `main` **only after** the human release owner explicitly approves the exact candidate. A green workflow or AI assessment is not an approval.
3. **Publication:** creation of a public tag, GitHub Release, package upload or external distribution requires **separate explicit owner authorization**, a final artifact identity and repeat validation against the promoted `main` commit. Promotion alone never grants publication permission.

Stop for any failed, missing or inconclusive mandatory gate. Do not silently downgrade a check to informational, substitute a run on another SHA, accept a self-review as independent review, or claim a documentation inventory is a substantive security audit.

## Required GitHub administrator settings

Create an **active** GitHub branch ruleset for `main` (Settings > Rules > Rulesets) and confirm its scope includes `refs/heads/main`.

- Require a pull request. Block direct pushes, force pushes and deletion. Require linear history and retain the repository's squash-only merge setting. Do not allow routine bypass of these rules.
- For this **single-maintainer repository**, set **required approvals to 0**. GitHub PR authors cannot approve their own changes; requiring a separate approval with no other authorized maintainer would indefinitely block routine releases. The owner instead performs and records a second-pass, risk-based self-review and explicit GO on the exact release candidate. This is **not** independent human review. Optional independent outside review is recommended for high-risk security/API changes; if another qualified reviewer joins later, revisit enforcing approvals. Require review conversations to be resolved where practicable; stale approval dismissal is useful only when approvals exist.
- Require checks at the latest promotion candidate. At the current workflow revision the **job check names**, not just workflow display names, are:
  - `Windows restore, MSBuild and NUnit` (workflow: Windows .NET Framework 4.8 build and test);
  - `Verify pinned packages and audit vulnerabilities` (same workflow);
  - `Standalone NuGet net48 consumer` (workflow: Verify internal release candidate in clean Windows consumer);
  - `Requirement catalogs and internal links` (workflow: Validate engineering documentation).
- Select the actual GitHub checks shown by a PR and pin their GitHub Actions source in the ruleset. Ensure all checks are required, not merely displayed as green. Prefer requiring the branch to be up to date with `main` or a merge queue if the repository supports it.
- Record the **sole maintainer's explicit GO** in the release-promotion PR after checking test evidence and a documented second-pass review. GitHub does not mechanically require the GO comment; it is an additional owner-controlled manual release gate, not a claim of technical enforcement.
- If appropriate, add a separate ruleset for `release/**` with the same squash, PR and CI requirements. Do not interfere with the documented source-promotion workflow without an owner-approved migration plan.

**GitHub administrator settings:** the ruleset above is verified active via the live GitHub API. The connected integration cannot modify rulesets; only the repository owner/administrator can do so. The independent human approval requirement was **removed deliberately**, not silently satisfied.

**Verify enforcement:** capture the active rule targeting `main` and required job contexts in GitHub; `main.protected = true` and the four mandatory checks are confirmed by the API. Verify a missing/failed check blocks the PR merge when a safe negative-case test is available; do not make unreviewed direct pushes just to test it. Record evidence in issue #199. A CI check being green is not proof of solo-maintainer risk acceptance or human GO.

## Pre-promotion candidate freeze and evidence

Record the following in the release-promotion PR **before** owner approval:

| Gate | Required evidence |
| --- | --- |
| Candidate identity | Integration branch, exact 40-character HEAD SHA, immutable change list, release scope and intentional deferrals. |
| Complete requirements | Functional/nonfunctional acceptance mapping, traceability, open issues classified as blocking, accepted risk or deferred. No unreviewed requirement scope expansion. |
| Solo-maintainer code review | Release owner records a **second-pass source review** with focus on security and input/error paths, dependency direction, API evolution, concurrency/ownership, resource limits, serialization and adversarial cases. Link findings, corrective issues, test evidence and accepted risks. AI-authored analysis is supporting evidence only; do not call it independent review. An outside reviewer is optional for high-risk changes. |
| Documentation verification | Correctness of current examples, limitations, license, API/binary contract, supported Windows/net48 and real package experience; inventory completeness **plus** semantic cross-checks. |
| Windows quality gate | All NUnit assemblies, 425-entry approved API baseline unless a deliberate revision is reviewed, seven JSON golden fixtures, XML docs, test metadata, style/architecture/resource gates, dependency/vulnerability audit. Exact workflow run URLs and SHA. |
| Installed consumers | Separate locally installed RC and stable `net48` packages, tested independently of project output, with version, runtime dependency, notice and consumer behavior checks. |
| Security and distribution | Investigate secrets scan scope/results and sensitive data, third-party licenses, pinned packages/advisories, hazardous CSV/spreadsheet use, XLSX resource/ZIP bounds and unsupported cases. Mark unverified claims explicitly. |
| Release artifacts | Stable version choice and nuspec metadata, repository ref/commit identity, generated assembly/API versions, final package metadata and checksum; do not treat temporary CI package hashes as distribution evidence. |
| Recovery | Publication halt, corrective version/revert procedure, dependency recall and consumer communication where applicable. |
| Human GO | Sole maintainer's explicit, dated GO for **source promotion** identifying exact candidate SHA, completed second-pass review and accepted risks; separately recorded publication GO if/when public distribution is requested. This GO is a manual procedural gate, not a GitHub required status check. |

Freeze the candidate once evidence and the owner's documented review are assembled. Any change to code, tests, workflows, version metadata or documents on the release branch **invalidates the previous candidate GO and its SHA-specific evidence**; rerun affected checks, update the evidence record and obtain a fresh owner GO. Do not auto-merge without a current GO for promotion.

Use this record in the promotion PR, replacing each placeholder with real links and values. Do **not** pre-check the boxes:

```text
Release/candidate:
Integration commit (40 hex):
Main base commit (40 hex):
Source diff reviewed by:
Requirements and deferred issues:
Code/security second-pass self-review by owner + findings URLs:
Documentation semantic review by owner + findings URLs:
Optional external review (if obtained; otherwise 'none'):
Windows build/NUnit/API/security run + SHA:
Clean RC/stable package consumer run + SHA:
Documentation run + SHA:
Issue/risk register (blocking, accepted, deferred):
Candidate freeze confirmed at:
Release-owner source-promotion GO + date + PR comment:
Squash-merge resulting main commit:
Postmerge Windows/consumer/docs runs on exact main commit:
GitHub ruleset ID and four required checks verified:
Separately authorized publication scope (tag/release/NuGet/none):
Publication artifact checksum and independent consumer result:
```

## Existing source promotion vs new policy

On 2026-10-31, release 0.1 was source-promoted through [PR #198](https://github.com/infoconex/RecordSift/pull/198) to `main` commit `26facaff671599d20ff6e8a1745d0d01450b0757`. Postmerge Windows, clean installed consumer and documentation workflows passed. Those results remain historical evidence; the promotion preceded the new ruleset and had no external reviewer approval. Solo maintenance **does not require** a separate reviewer, but must not misrepresent a self-review as independent. **Do not retroactively assert the new protected-branch process was met** or rewrite a valid source commit merely to simulate compliance.

No public 0.1 release, tag or NuGet upload is authorized. New review findings and release-governance controls must be resolved or explicitly accepted before a publication GO. If fixing a defect changes the release candidate, use an issue branch, reviewed PR into `release/0.1`, and a **new separately authorized squash promotion**; never silently edit `main`.
