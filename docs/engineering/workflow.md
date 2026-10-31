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

When all release scope, complete documentation review, and quality gates pass, open a **separate release PR** from `release/0.1` into `main`. Require all three Windows/consumer/docs workflows to pass on the promotion PR; **SQUASH merge** the release PR to produce **one consolidated `main` commit** and reverify all three workflows on that exact SHA. Record final SDK, framework, dependency and test evidence. **Promotion does not authorize creating a tag, GitHub release, or uploading a NuGet package**; those actions need explicit, separate owner authorization.

## Risk controls

- Direct commits to `main` or `release/0.1` should be avoided after initialization.
- Branch protection and merge-method enforcement are *not yet claimed as configured*; these are repository-admin follow-up settings.
- Do not open or merge competing PRs that silently alter the same requirement contract.
- Keep historical configuration deliberately constructed and plainly labeled.
- An untested PR is not release-ready, even if a documentation check passes.

## Definition of done (per issue)

The documented requirement and acceptance scenarios are addressed; executable tests are added or evidence limitations are recorded; tests pass on supported environments; any changes to contracts, dependency graphs, and traceability are documented; the PR receives review and is squash-merged.
