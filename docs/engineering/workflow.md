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

When all release scope and quality gates pass, open a **separate release PR** from `release/0.1` into `main`. Record final SDK, framework, NuGet dependency and test evidence. Tag the accepted release on `main` and retain intentional baseline/modernization milestone tags.

## Risk controls

- Direct commits to `main` or `release/0.1` should be avoided after initialization.
- Branch protection and merge-method enforcement are *not yet claimed as configured*; these are repository-admin follow-up settings.
- Do not open or merge competing PRs that silently alter the same requirement contract.
- Keep historical configuration deliberately constructed and plainly labeled.
- An untested PR is not release-ready, even if a documentation check passes.

## Definition of done (per issue)

The documented requirement and acceptance scenarios are addressed; executable tests are added or evidence limitations are recorded; tests pass on supported environments; any changes to contracts, dependency graphs, and traceability are documented; the PR receives review and is squash-merged.
