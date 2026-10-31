# ADR 0019 — Approved framework dependency allowlists for Domain and Application

**Status:** Accepted through [engineering issue #108](https://github.com/infoconex/RecordSift/issues/108). Extends [ADR 0013](0013-physical-domain-application-infrastructure-assemblies.md) and [ADR 0018](0018-independent-domain-application-tests.md) without changing runtime code.

## Problem

The baseline project graph already prevents outward `ProjectReference` dependencies. However, the original assembly check permitted any Domain reference whose name started with `System`, and the Application check explicitly searched for `CsvHelper`. A new unrelated vendor package, fake `System.*` assembly, package build targets or hint path could therefore bypass the intent of isolation without changing the project graph.

## Decision

For current .NET Framework 4.8 / C# 7.3 Domain and Application projects, explicitly allow only the following:

- Built-in .NET Framework assembly references: `System` and `System.Core` (exact names).
- The standard traditional build import: `$(MSBuildToolsPath)\Microsoft.CSharp.targets`.
- Existing approved inward `ProjectReference` edges: Application -> Domain; Domain -> none.

Reject **all other** `Reference` identities, including `System.Fake`, and all explicit `HintPath` entries even when attached to an allowed name. Reject package manifests, `PackageReference`, `PackageDownload`, `PackageVersion`, NuGet restore-source/project-style metadata, analyzers, COM references, `UsingTask`, and unexpected `Import` or SDK attributes in the core project definitions. Reject physical `packages.config` under either core project.

This is a deliberately narrow **current-policy allowlist**, not a claim that every .NET Framework assembly is architecturally unsuitable. Any newly required standard framework assembly should be reviewed, documented and added deliberately. Third-party integrations belong in Infrastructure; any exception requires a separate issue and explicit acceptance.

## Test-first enforcement

The first commit on the issue adds positive approved-reference and negative vendor metadata fixtures and demonstrates a **red** architecture workflow. The implementation makes the fixtures **green** and checks the actual project XML/physical package manifests, independently of the existing dependency-direction graph and the Windows NuGet package audit.

These checks constrain explicit traditional project metadata and imports. They are not a complete security sandbox and cannot detect runtime reflection, dynamically downloaded binaries, or undisclosed external system-wide MSBuild modifications. Existing CI continues to compile and test the code; the package audit applies to pinned Infrastructure dependencies.

No production API, parser behavior, package versions or FR/NFR acceptance status changes.
