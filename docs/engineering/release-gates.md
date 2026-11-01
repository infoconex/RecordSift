# Release promotion and publication governance

**Status: proposed policy; not yet enforced by GitHub settings.** Tracked by [issue #199](https://github.com/infoconex/RecordSift/issues/199). The repository presently reports `main.protected = false` and no rulesets (checked 2026-10-31). This document does **not** claim branch protection or reviewer controls are already active. [Release 0.1 preliminary audit](release-0.1-prepublication-audit.md).

## Separate decisions

1. **Issue integration:** a reviewed issue branch is squash-merged into `release/0.1` after appropriate checks. The integration branch is not a released build.
2. **Source promotion:** a frozen, reviewed release candidate is squash-merged to `main` **only after** the human release owner explicitly approves the exact candidate. A green workflow or AI assessment is not an approval.
3. **Publication:** creation of a public tag, GitHub Release, package upload or external distribution requires **separate explicit owner authorization**, a final artifact identity and repeat validation against the promoted `main` commit. Promotion alone never grants publication permission.

Stop for any failed, missing or inconclusive mandatory gate. Do not silently downgrade a check to informational, substitute a run on another SHA, accept a self-review as independent review, or claim a documentation inventory is a substantive security audit.

## Required GitHub administrator settings

Create an **active** GitHub branch ruleset for `main` (Settings > Rules > Rulesets) and confirm its scope includes `refs/heads/main`.

- Require a pull request. Block direct pushes, force pushes and deletion. Require linear history and retain the repository's squash-only merge setting. Do not allow routine bypass of these rules.
- Require at least **one approval from a reviewer independent of the change author**. Two independent approvals are recommended for an unusually broad release promotion. Dismiss stale approvals after changes and require all review conversations resolved.
- Require checks at the latest promotion candidate. At the current workflow revision the **job check names**, not just workflow display names, are:
  - `Windows restore, MSBuild and NUnit` (workflow: Windows .NET Framework 4.8 build and test);
  - `Verify pinned packages and audit vulnerabilities` (same workflow);
  - `Standalone NuGet net48 consumer` (workflow: Verify internal release candidate in clean Windows consumer);
  - `Requirement catalogs and internal links` (workflow: Validate engineering documentation).
- Select the actual GitHub checks shown by a PR and pin their GitHub Actions source in the ruleset. Ensure all checks are required, not merely displayed as green. Prefer requiring the branch to be up to date with `main` or a merge queue if the repository supports it.
- Configure an appropriate release-owner approval convention; record the owner GO explicitly in the release-promotion PR, distinct from independent technical review.
- If appropriate, add a separate ruleset for `release/**` with the same squash, PR and CI requirements. Do not interfere with the documented source-promotion workflow without an owner-approved migration plan.

**Administrator-only:** the connected GitHub integration cannot change branch protection or active rulesets. The release owner or a GitHub repository administrator must apply these settings. Do not mark this item complete based on documentation changes.

**Verify enforcement:** capture the active rule and applicable branch from the GitHub settings UI; verify `main` is reported protected or an active equivalent ruleset is confirmed; demonstrate that a PR without required review or a required check cannot merge, and that direct-push permission is blocked. Record evidence links in issue #199. A check merely being available is not proof that it is required.

## Pre-promotion candidate freeze and evidence

Record the following in the release-promotion PR **before** owner approval:

| Gate | Required evidence |
| --- | --- |
| Candidate identity | Integration branch, exact 40-character HEAD SHA, immutable change list, release scope and intentional deferrals. |
| Complete requirements | Functional/nonfunctional acceptance mapping, traceability, open issues classified as blocking, accepted risk or deferred. No unreviewed requirement scope expansion. |
| Independent code review | Named reviewer(s) and approval links; security and input/error-path assessment, dependency direction, public API evolution, concurrency/ownership, resource limits, serialization and adversarial cases. AI-authored analysis is supporting evidence only. |
| Documentation verification | Correctness of current examples, limitations, license, API/binary contract, supported Windows/net48 and real package experience; inventory completeness **plus** semantic cross-checks. |
| Windows quality gate | All NUnit assemblies, 425-entry approved API baseline unless a deliberate revision is reviewed, seven JSON golden fixtures, XML docs, test metadata, style/architecture/resource gates, dependency/vulnerability audit. Exact workflow run URLs and SHA. |
| Installed consumers | Separate locally installed RC and stable `net48` packages, tested independently of project output, with version, runtime dependency, notice and consumer behavior checks. |
| Security and distribution | Investigate secrets scan scope/results and sensitive data, third-party licenses, pinned packages/advisories, hazardous CSV/spreadsheet use, XLSX resource/ZIP bounds and unsupported cases. Mark unverified claims explicitly. |
| Release artifacts | Stable version choice and nuspec metadata, repository ref/commit identity, generated assembly/API versions, final package metadata and checksum; do not treat temporary CI package hashes as distribution evidence. |
| Recovery | Publication halt, corrective version/revert procedure, dependency recall and consumer communication where applicable. |
| Human GO | Independent review approvals, explicit release-owner GO for **source promotion**, and a separately recorded publication GO if/when public distribution is requested. |

Freeze the candidate once evidence and approvals are assembled. Any change to code, tests, workflows, version metadata or documents on the release branch **invalidates the previous candidate approval and its SHA-specific evidence**; rerun affected checks, update the evidence record and obtain fresh review/GO. Do not auto-merge while review is pending.

Use this record in the promotion PR, replacing each placeholder with real links and values. Do **not** pre-check the boxes:

```text
Release/candidate:
Integration commit (40 hex):
Main base commit (40 hex):
Source diff reviewed by:
Requirements and deferred issues:
Code/security independent reviewer + review URL:
Documentation reviewer + review URL:
Windows build/NUnit/API/security run + SHA:
Clean RC/stable package consumer run + SHA:
Documentation run + SHA:
Issue/risk register (blocking, accepted, deferred):
Candidate freeze confirmed at:
Release-owner source-promotion GO + date + PR comment:
Squash-merge resulting main commit:
Postmerge Windows/consumer/docs runs on exact main commit:
Separately authorized publication scope (tag/release/NuGet/none):
Publication artifact checksum and independent consumer result:
```

## Existing source promotion vs new policy

On 2026-10-31, release 0.1 was source-promoted through [PR #198](https://github.com/infoconex/RecordSift/pull/198) to `main` commit `26facaff671599d20ff6e8a1745d0d01450b0757`. Postmerge Windows, clean installed consumer and documentation workflows passed. Those results remain historical evidence; the promotion had no independent approval recorded, and the branch had no enforced protection. **Do not retroactively assert this new policy was met** or rewrite a valid source commit merely to simulate compliance.

No public 0.1 release, tag or NuGet upload is authorized. New review findings and release-governance controls must be resolved or explicitly accepted before a publication GO. If fixing a defect changes the release candidate, use an issue branch, reviewed PR into `release/0.1`, and a **new separately authorized squash promotion**; never silently edit `main`.
