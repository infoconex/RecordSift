# RecordSift release 0.1 compatibility baseline

This directory contains an **approved, human-readable .NET Framework 4.8 public API baseline**, not a claim that a future framework has been verified.

## Compiled API and package gate (NFR-010-SC01)

`scripts/verify_public_api.ps1` is executed by the Windows build after Release MSBuild. It loads all five production DLLs using .NET Framework Windows PowerShell, enumerates their exported types and declared public members with reflection, and normalizes signatures into ordinally sorted lines. It includes type visibility/kind, constructors, methods (including generic constraints and parameters), properties (including public setter presence), events, fields, assembly identity/file version, selected assembly metadata, project target/output kind and pinned direct package dependencies.

It compares the result against `public-api-net48.txt`. A missing baseline or changed line fails CI; differences are printed for reviewers. To inspect a candidate baseline after a Release build, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/verify_public_api.ps1 -PrintCurrent
```

Commit intentional baseline changes **only in a reviewed PR** explaining API/packaging impact. Do not regenerate the approved baseline automatically during normal CI, do not add public members merely to placate the check, and never update the baseline to hide an unreviewed regression. The check is primarily source/binary *shape* protection; it cannot prove runtime semantics or cross-framework binary binding by itself.

## JSON compatibility gate (NFR-010-SC02)

`tests/RecordSift.Infrastructure.Reporting.Tests/Fixtures/Compatibility/` holds seven small JSON golden files compared in `JsonCompatibilityGoldenTests`. They are not copies of the normative schema. The test compares all JSON properties, value types, null/omission policy and ordered diagnostic arrays, and checks deterministic repeated serialization. It uses the real Infrastructure serializer against valid in-memory Domain outcomes; CSV and XLSX adapter-specific import behavior is separately tested in their respective suites.

## Future modernization obligation

The present approved surface and fixtures are from **net48**. Any migration to another target framework must perform a cross-target public API and package/binding comparison and re-run semantic JSON fixture checks against the modernized implementation before claiming compatibility. A passing net48-only gate does not satisfy that future milestone.
