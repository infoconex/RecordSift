# GitHub issue, branch and release workflow

## One requirement, one issue, one working branch

- Use the stable FR-/NFR- requirement ID and a corresponding GitHub issue.
- Engineering infrastructure tasks also use independent issues.
- Only start a branch when an issue is actively being implemented; use `issue/<number>-<slug>` branching from the current `release/0.1`.
- Each issue PR **targets release/0.1**, references the primary issue, and updates traceability when relevant.
- Review CI and evidence, then **squash merge**. Close the issue after the merge is verified.
- Do not assume GitHub automatically closes issues when PRs merge to a non-default release branch.
- Delete the issue branch after merging when safe.

## Release 0.1 integration

```text
main
└── release/0.1
    ├── issue/2-fr-001       → PR / squash into release/0.1
    ├── issue/6-fr-002       → PR / squash into release/0.1
    └── other issue branches → PR / squash into release/0.1
```

When all release scope, complete documentation review and quality gates pass, open a **separate release PR** from `release/0.1` into `main`. First freeze the candidate and satisfy the [formal promotion governance](release-gates.md): active `main` ruleset with required job checks (zero reviewer approvals for sole maintainer), a documented owner second-pass technical/security/consumer review, and explicit release-owner GO **for that candidate SHA**. Require all three Windows/consumer/docs workflows (four GitHub job checks) on the promotion PR. Only then **SQUASH merge** the release PR to produce **one consolidated `main` commit**, and reverify all jobs on that SHA. Missing branch enforcement, missing required CI evidence, or missing documented owner GO is a **STOP** condition. Do not require an unavailable independent PR approval or mislabel a self-review as independent. Record SDK, framework, dependency and test evidence. **Promotion does not authorize creating a tag, GitHub release, or uploading a NuGet package**; those actions need explicit, separate owner authorization.

## Risk controls

- Direct commits to `main` or `release/0.1` must be blocked by active rulesets after their administrator configuration.
- As of 2026-10-31, `main` has active [ruleset #26295899](https://github.com/infoconex/RecordSift/rules/26295899), four mandatory GitHub Actions checks, squash-only PRs and no bypass actors. Sole-maintainer required approvals intentionally remain 0. [Issue #199](https://github.com/infoconex/RecordSift/issues/199) tracks remaining sign-off and enforcement evidence. Do **not** mistake successful CI for the owner's release GO.
- Do not open or merge competing PRs that silently alter the same requirement contract.
- Keep historical configuration deliberately constructed and plainly labeled.
- An untested PR is not release-ready, even if a documentation check passes.

## Definition of done (per issue)

The documented requirement and acceptance scenarios are addressed; executable tests are added or evidence limitations are recorded; tests pass on supported environments; any changes to contracts, dependency graphs, and traceability are documented; the PR receives review and is squash-merged.
