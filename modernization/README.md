# RecordSift modernization case study

**Baseline source:** [RecordSift main at 3627a62260437db0bde83c5c7d3be56caee8af95](https://github.com/infoconex/RecordSift/commit/3627a62260437db0bde83c5c7d3be56caee8af95).

This directory holds focused modernization use cases that can be reviewed independently, with a before/after diff and reproducible execution evidence. The `modernization/case-study` branch is a separate educational track from RecordSift's active release branches. Stage ordering/numbers will be assigned as the series roadmap is finalized.

## Stages

- [nuget.config — explicitly control inherited disabled package sources](stages/nuget-config/README.md): a narrowly scoped, reversible configuration change supported by EXP-044

Merge each approved stage into the case-study branch only after review. Do not promote these changes into `main` or a release without separate owner authorization.
