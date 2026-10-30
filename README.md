# RecordSift

**Import, validate, and report on structured data.**

RecordSift is a planned open-source .NET library for importing records from CSV and Excel files, applying a consistent validation model, and producing actionable diagnostics and JSON reports.

## Project status

**Release 0.1 is in planning.** This repository does not yet contain a working library or published NuGet package.

The initial functional baseline is intended to target .NET Framework 4.8, using traditional (non-SDK-style) project files and real NuGet dependencies. The historical project configuration will be **deliberately constructed for a documented modernization experiment**, not presented as an authentic legacy codebase.

Planned modernization experiments will distinguish changes to project configuration, NuGet dependency management, the build SDK, and the target framework. In particular, a .NET 8 target will later be evaluated using the .NET 10 SDK without changing the target framework.

## Project requirements

- [Requirements, specifications, and engineering documentation](docs/README.md)
- [Functional requirements](docs/requirements/functional.md)
- [Nonfunctional requirements](docs/requirements/non-functional.md)

## Development workflow

- `main` is reserved for accepted release integrations.
- `release/0.1` is the integration branch for the 0.1 release.
- Each issue is implemented on a short-lived branch created from `release/0.1`.
- Issue branches are reviewed with a pull request targeting `release/0.1` and **squash-merged**.
- When 0.1 meets its acceptance criteria, open a release pull request from `release/0.1` into `main`.
- Do not merge an unfinished release into `main`.

## Initial scope

The first functional release aims to provide CSV and Excel import, common field-level validation, row/source diagnostics, and JSON validation reports. Package choices and exact versions will be recorded and tested before the functional baseline is declared complete.
