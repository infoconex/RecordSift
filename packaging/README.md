# RecordSift 0.1.0-rc.1 (internal validation only)

**Do not redistribute or publish this candidate.** The RecordSift distribution license is awaiting repository owner approval in [issue #186](https://github.com/infoconex/RecordSift/issues/186). Public release metadata and approved license text must be added before publication.

RecordSift is currently supported on **Windows, .NET Framework 4.8, C# 7.3** only. This internal NuGet package groups five RecordSift assemblies (Domain, Application, CSV, XLSX, Reporting) and their XML documentation into one `lib/net48` folder. It declares exact CsvHelper 33.1.0 and ExcelDataReader 3.9.0 package dependencies; NuGet resolves their platform-specific dependency graphs. No Office installation or network service is needed at runtime.

Use a local package source during internal testing, then reference `RecordSift` version `0.1.0-rc.1` in a net48 consumer. Imports are synchronous and in-memory; callers must configure the existing input resource limits and treat diagnostic identifiers as untrusted data. See [the repository README](https://github.com/infoconex/RecordSift/blob/release/0.1/README.md) for current import samples. The package is *not* published to nuget.org.

To rebuild from a clean Windows environment: restore `RecordSift.sln` with `NuGet.Config`, compile `Release|Any CPU` using MSBuild, and run `scripts/build_release_candidate.ps1`. Windows CI checks the compiled API baseline and internal package; package hashes may differ between builds due to ZIP timestamps. Packaging does not change any import behavior.
