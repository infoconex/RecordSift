# GitHub Copilot repository instructions

Follow [AGENTS.md](../AGENTS.md) and the [requirements index](../docs/README.md).

- Work on one issue at a time on `issue/<number>-<slug>` created from the active `release/0.1` branch.
- Start from the authoritative `docs/scenarios/` Markdown for the issue; use TDD and provide explicit scenario-to-test traceability.
- Prefer minimal, behavior-preserving changes; do not combine a dependency upgrade with project-format modernization in a single experiment.
- Test generated code with actual commands/CI and report **observed**, not hypothetical, outcomes.
- Treat CSV/XLSX files as untrusted and avoid logging sensitive source values.
- Before proposing completion, verify the PR targets `release/0.1`, includes acceptance evidence, and uses squash merge.
