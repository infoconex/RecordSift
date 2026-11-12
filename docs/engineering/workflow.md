# GitHub issue, branch and release workflow

## One requirement, one issue, one working branch

- Use the stable FR-/NFR- requirement ID and a corresponding GitHub issue.
- Engineering infrastructure tasks also use independent issues.
- **Current workflow (November 11, 2026):** only `main` and `modernization/case-study` are active long-lived branches. `release/0.1` and `release/0.2` were retired after their relevant work was preserved; references below to earlier release branches are historical.
- Start `issue/<number>-<slug>` from current `main` only when an issue is actively being implemented. Each ordinary issue PR **targets protected `main`**, references the issue and updates traceability when relevant.
- Run the required Windows, consumer and documentation checks and perform the relevant technical/security review. Obtain the required owner approval for the exact PR candidate, then **squash merge** and verify postmerge checks before closing the issue.
- `modernization/case-study` is isolated case-study work; do not target ordinary implementation PRs there or silently merge it into `main`.
- A future maintenance or release branch (including `release/0.1.x`) must be created only after a separate owner scope/base decision and must use the formal source-promotion approval process. No public release is implied.
- Delete short-lived issue branches after verified merges when safe.

## Historical release 0.1 integration (completed)

The following diagram records how the original 0.1 release was developed; it is **not** the workflow for new issues.

```text
main
└── release/0.1
    ├── issue/2-fr-001       → PR / squash into release/0.1
    ├── issue/6-fr-002       → PR / squash into release/0.1
    └── other issue branches → PR / squash into release/0.1
```

## Current source-promotion policy

For a new release candidate or separately authorized maintenance fix involving a dedicated integration or maintenance branch, open a **separate source-promotion PR** from the reviewed branch into `main`. Ordinary issue PRs now target `main` directly, but must still satisfy applicable main-branch protections, reviews and owner decisions; a merged issue PR is not a release or publication approval. First freeze the candidate and satisfy the [formal promotion governance](release-gates.md): active `main` ruleset with required job checks (zero reviewer approvals for sole maintainer), a documented owner second-pass technical/security/consumer review, and explicit release-owner GO **for that candidate SHA**. Require all three Windows/consumer/docs workflows (four GitHub job checks) on the promotion PR. Only then **SQUASH merge** the source-promotion PR to produce **one consolidated `main` commit**, and reverify all jobs on that SHA. Missing branch enforcement, missing required CI evidence, or missing documented owner GO is a **STOP** condition. Do not require an unavailable independent PR approval or mislabel a self-review as independent. Record SDK, framework, dependency and test evidence. **Promotion does not authorize creating a tag, GitHub release, or uploading a NuGet package**; those actions need explicit, separate owner authorization.

## Risk controls

- `main` is protected by an active PR-required ruleset; do not commit directly to it. Use reviewed PRs for integration and maintenance branches, and verify their protection settings separately rather than assuming they match `main`.
- As of 2026-10-31, `main` has active [ruleset #26295899](https://github.com/infoconex/RecordSift/rules/26295899), four mandatory GitHub Actions checks, squash-only PRs and no bypass actors. Sole-maintainer required approvals intentionally remain 0. [Issue #199](https://github.com/infoconex/RecordSift/issues/199) tracks remaining sign-off and enforcement evidence. Do **not** mistake successful CI for the owner's release GO.
- Do not open or merge competing PRs that silently alter the same requirement contract.
- Keep historical configuration deliberately constructed and plainly labeled.
- An untested PR is not release-ready, even if a documentation check passes.

## Definition of done (per issue)

The documented requirement and acceptance scenarios are addressed; executable tests are added or evidence limitations are recorded; tests pass on supported environments; any changes to contracts, dependency graphs, and traceability are documented; the PR receives review and is squash-merged.
