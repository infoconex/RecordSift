# Unpublished 0.1 candidate packaging decision (issue #183)

**Decision:** Build one package `RecordSift.0.1.0-rc.1.nupkg` containing the five RecordSift net48 assemblies and their generated XML documentation. Use the existing NuGet CLI and MSBuild; no SDK-style conversion, installer, new build system or publication. A standalone consumer must install this local package before any GO decision.

**Version correction:** Infrastructure.Csv and Infrastructure.Xlsx lacked explicit assembly versions, yielding `0.0.0.0` while the other three binaries used `0.1.0.0`. Their only intentional compiled public-API baseline changes are those two assembly identity versions and two file versions. No public types or methods change; the first public package has not shipped, so there is no external legacy binding commitment. The approved `docs/compatibility/public-api-net48.txt` snapshot was adjusted accordingly and remains gated by Windows reflection verification.

**Dependency policy:** NuGet installs the exact declared direct dependencies CsvHelper 33.1.0 and ExcelDataReader 3.9.0 with their transitive dependencies; avoid bundling vendor binaries as RecordSift's own files. Exact pinned versions for the existing development projects remain unchanged. Runtime assembly deployment is delegated to NuGet. A clean consumer must verify this graph rather than assume a source-build output is distributable.

**Reproduction:** Run `nuget restore RecordSift.sln -NonInteractive -PackagesDirectory packages -ConfigFile NuGet.Config`, then `msbuild RecordSift.sln /m /t:Rebuild /p:Configuration=Release '/p:Platform=Any CPU'`, followed by `powershell -File scripts/build_release_candidate.ps1 -OutputDirectory <temporary path>`. The script fails when DLLs/XML docs or version consistency are missing and inspects package entries. Repeatable procedure does **not** imply byte-for-byte reproducible ZIP archives; NuGet archive timestamps and build environment can affect the hash.

**Release gate:** No root project license has been approved (issue #186). The package lacks approved license metadata by design and is for unpublished CI/local verification **only**. Before public distribution, approve a source license, add its text and `.nuspec` license metadata, sign off on vendor license notices, regenerate an appropriate stable release version, and repeat consumer/security acceptance.

**No cross-platform promise:** Only Windows/.NET Framework 4.8 is supported. `main` promotion and package publication remain separately authorized steps.
