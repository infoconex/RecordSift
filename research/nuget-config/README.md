# NuGet.Config inheritance research (article evidence)

**Tracking:** [dotnet-modernization issue #87](https://github.com/infoconex/dotnet-modernization/issues/87)  
**Scope:** Isolated research-only branch. Not a RecordSift product change, source-policy adoption, release or merge authorization.

## Questions

1. Does a child `<packageSources><clear /></packageSources>` exclude an inherited parent package source?
2. Does a child `<disabledPackageSources><clear /></disabledPackageSources>` clear an inherited setting that disables an otherwise explicitly declared source?
3. Does a normal `nuget restore` **without** `-ConfigFile` succeed against the checked-in root `NuGet.Config` using a fresh solution-local package directory?

## Reproduce

On Windows with a .NET SDK supporting `dotnet nuget list source`, NuGet CLI and PowerShell 7:

```powershell
pwsh -NoProfile -File .\research\nuget-config\scripts\verify-inheritance.ps1 -CheckRestore
```

The script creates parent/child configuration in a disposable OS temporary directory, executes A/B/C/D source-listing controls, and optionally restores RecordSift from the repository root without `-ConfigFile`. It writes `TestResults/nuget-config-inheritance/results.json`, `inheritance-output.txt`, and separate restore/source logs, then deletes the temporary configuration. Review resulting source URLs before sharing logs.

GitHub Actions workflow: `.github/workflows/research-nuget-config-inheritance.yml`. The artifact stores complete observed output and tool versions. A green workflow is not proof of private-feed security, audit-source contact, package provenance, full reproducibility or permission to change RecordSift's `NuGet.Config`.

**Evidence disposition:** Pending execution and review. Interpret the executed output and any runner-level sources before recording findings in the modernization research repository.
