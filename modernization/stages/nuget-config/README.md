# Modernization use case — NuGet.Config source policy

**Tracking:** [RecordSift #267](https://github.com/infoconex/RecordSift/issues/267).  
**Research:** [EXP-044 — NuGet source inheritance](https://github.com/infoconex/dotnet-modernization/blob/research/nuget-config-article-evidence-20261109/projects/recordsift/evaluations/2026-11-pre-sdk/experiments/EXP-044-nuget-config-source-inheritance.md).  
**Immutable before-state:** [RecordSift `main` @ `3627a62260437db0bde83c5c7d3be56caee8af95`](https://github.com/infoconex/RecordSift/commit/3627a62260437db0bde83c5c7d3be56caee8af95).  
**Before-file:** [NuGet.Config at baseline](https://github.com/infoconex/RecordSift/blob/3627a62260437db0bde83c5c7d3be56caee8af95/NuGet.Config).  
**Case-study branch:** `modernization/case-study`, intentionally separate from active release work.  
**Focused stage branch:** `modernization/nuget-config`. Neither is authorized for promotion to `main`.

## Decision and scope

**Hypothesis:** RecordSift's existing `<packageSources><clear /></packageSources>` removes inherited package feeds but does not remove an inherited `disabledPackageSources` entry that disables `nuget.org`. The source-controlled policy should explicitly declare which sources are enabled for this independent modernization example.

**One behavioral change:** Insert an empty `disabledPackageSources` collection with `<clear />` between existing `packageSources` and `auditSources` in root `NuGet.Config`.

**Unchanged:** .NET Framework 4.8, project format, `packages.config`, NuGet package IDs and versions, `packageSources`, `auditSources`, public API and application code.

**Trade-off:** Clearing inherited disabled sources overrides inherited disable settings, including an organization's deliberately disabled feed. This stage is appropriate only because the case study explicitly permits restoring from nuget.org. An enterprise repository must first comply with its organization's source restrictions.

## Before and after

Before, the repository explicitly listed nuget.org, yet an inherited `disabledPackageSources` entry could disable it.

After, the source-controlled `NuGet.Config` independently clears `packageSources` and `disabledPackageSources`, while preserving a separately explicit `auditSources`.

For a directly reviewable diff, compare the [immutable baseline](https://github.com/infoconex/RecordSift/blob/3627a62260437db0bde83c5c7d3be56caee8af95/NuGet.Config) with [stage NuGet.Config](https://github.com/infoconex/RecordSift/blob/modernization/nuget-config/NuGet.Config). The stage remains a candidate until its review and explicit merge approval.

## Local and CI parity

On Windows with .NET SDK 8.0.2xx or newer, NuGet CLI and PowerShell 7, from the repository root:

```powershell
pwsh -NoProfile -File .\modernization\stages\nuget-config\scripts\verify.ps1 -CheckRestore
```

The same script is invoked by the dedicated Windows workflow `.github/workflows/modernization-nuget-config.yml`.

The script:
1. Validates that expected source, disabled-source, and audit-source elements are present in the checked-in XML.
2. Creates disposable parent and child NuGet configurations; the parent disables nuget.org and adds a test feed.
3. Checks **positive** behavior against the actual new configuration: only nuget.org remains enabled.
4. Runs the **negative control** after removing the disabled-source clear in a disposable copy: nuget.org becomes disabled.
5. Restores the original configuration and confirms nuget.org is enabled again.
6. Checks normal source discovery and runs `nuget restore` without `-ConfigFile` into a fresh package directory.

Run artifacts are written under `TestResults/modernization-nuget-config` with `results.json`, `effective-sources.txt`, and `restore.log`. Review outputs for local feed names before sharing.

A successful restore does not prove full deterministic builds, package provenance, active security audit contact, or policy enforcement outside the tested environment. Full product compatibility remains established by the existing app baseline, not this narrow stage check.

## Outcome

**Pending exact-stage CI execution and review.** Link the successful run and observed version/output when obtained. No merge, release or blog publication is implied by the presence of this stage.
