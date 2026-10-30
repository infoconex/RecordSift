# ADR 0021 — Isolate CSV infrastructure classes by responsibility

**Status:** Accepted through [engineering issue #106](https://github.com/infoconex/RecordSift/issues/106). Extends [ADR 0014](0014-infrastructure-csv-adapter-naming.md) without changing the public API, technology choices or net48 runtime target.

## Context

The Infrastructure.Csv implementation had four C# classes in one file: the public `CsvRecordReaderAdapter`, the internal character-budget `TextReader`, a malformed-CSV exception carrying a physical line and an internal character-limit exception. These are all correctly Infrastructure-owned, but have distinct reasons to change. Keeping four classes in a single adapter source hides those responsibilities and unnecessarily couples maintenance.

## Decision

Maintain one `RecordSift.Infrastructure.Csv` production assembly and the same namespace and `ICsvRecordReader` implementation:

- `CsvRecordReaderAdapter.cs`: **only** the public CsvHelper-backed port adapter and its existing private CSV physical-line counting method.
- `CharacterLimitedTextReader.cs`: **only** the internal decoded-character budget wrapper. Preserves exact-budget EOF behavior, bounded read checks and non-disposal of caller-owned `TextReader`.
- `MalformedCsvException.cs`: **only** the internal source-structure exception preserving the one-based physical line.
- `CharacterLimitExceededException.cs`: **only** the internal character-limit exception.

No extra public interfaces, packages, utility layers or speculative abstractions are added; classes retain the same accessibility and implementation logic. The traditional .csproj explicitly compiles the four named files. Architecture validation checks each physical file declares exactly its intended class and no unexpected infrastructure source appears.

## Test-first evidence and invariants

A red architecture guard first demands four owned files and rejects a combined class file, missing source and improperly declared namespace. Green work splits the source and updates explicit MSBuild `Compile` inputs. All existing Domain, Application and real-adapter NUnit cases remain unchanged and must pass in the final Windows net48 build, including bounded reading, exact source budgets, physical-line provenance, malformed CSV, safe diagnostics, and not disposing caller-owned readers. Roslyn naming, embedded resource checks and pinned dependency audit also remain green.

No public contract, exception/diagnostic translation, source provenance, default value, parser vendor version, schema semantics or FR/NFR acceptance status changes.
