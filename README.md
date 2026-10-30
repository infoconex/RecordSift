# RecordSift

**Import, validate, and report on structured data.**

RecordSift is an open-source .NET library project intended to import records from CSV and Excel files, validate them, and produce actionable diagnostics and JSON reports.

## Project status

**Release 0.1 is under development.** A deliberately constructed, traditional .NET Framework 4.8 solution and executable NUnit smoke tests are introduced by engineering issue #57. The FR-030 in-memory summary/status model has automated unit coverage. No CSV/XLSX importer, end-to-end requirement verification, or published NuGet package is claimed yet.

The historical project configuration exists for a documented modernization experiment, not as recovered legacy production code. Subsequent experiments will separately change NuGet dependency management, project format, build SDK and target framework.

## Development baseline

- [Historical net48 build and test instructions](docs/engineering/baseline.md)
- [Requirements, specifications, and engineering documentation](docs/README.md)
- [Functional requirements](docs/requirements/functional.md)
- [Nonfunctional requirements](docs/requirements/non-functional.md)
- [Acceptance scenario specifications](docs/scenarios/README.md)

## Development workflow

- `main` is reserved for accepted releases.
- `release/0.1` is the integration branch for the 0.1 release.
- Each issue uses an independent short-lived branch from `release/0.1`, reviewed by PR and squash-merged.
- Requirements remain `Planned` until linked executable acceptance evidence exists.
- When release 0.1 passes its quality gates, open a separate PR from `release/0.1` to `main`.

## Initial scope

FR-030 provides immutable in-memory status and row counters, with passive supplied error diagnostics. CSV/XLSX ingestion, schema validation, error generation, typed accepted records and JSON report serialization remain independently tracked and unimplemented.
