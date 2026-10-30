# ADR 0012 — Enforce Microsoft C# naming in historical .NET Framework builds

**Status:** Accepted in [engineering issue #93](https://github.com/infoconex/RecordSift/issues/93).

## Context

RecordSift's historical .NET Framework 4.8 non-SDK projects intentionally avoid upgrades that obscure modernization comparisons. Prior `.editorconfig` had only interface-naming suggestions, with no mandatory naming analyzer gate. Microsoft recommends PascalCase for all C# constants including local and private names; stable protocol string values such as `SOURCE_UNREADABLE` must not be confused with the names of C# variables.

## Decision

1. Specify Roslyn naming rules in the root `.editorconfig`: constant names PascalCase; interfaces `I` + PascalCase; public types/members PascalCase; parameters and ordinary locals camelCase; private nonconstant instance fields `_camelCase`; private static nonconstant fields `s_camelCase`. Constant-field rules have explicit higher-priority private intersections to prevent `s_` from being applied to `const` fields. Source projects use the full member naming rules. Test methods retain `FR_###_SC##` / `NFR_###_SC##` prefixes for immutable scenario traceability.
2. Run `dotnet format style RecordSift.sln --no-restore --verify-no-changes --diagnostics IDE1006 --severity error --verbosity normal` in mandatory Windows CI after traditional MSBuild. This uses the installed .NET SDK **as a tooling host only**, preserving the net48 target, C# 7.3 language version and existing runtime dependencies.
3. Add `scripts/validate_constant_names.py` as a dependency-free narrow complementary check. Roslyn IDE1006 may reject lower-case const declarations but not always `UPPER_SNAKE_CASE` identifiers; guard those spellings directly, with built-in negative/positive fixtures executed in docs CI. This check is intentionally not a replacement for semantic Roslyn analysis.
4. No public types, diagnostic identifiers, string values, resource keys, result JSON schema, parser packages, or runtime business rules change. An internal static resource cache is renamed `s_catalog`, and a local test constant `Secret` becomes PascalCase.
5. No new non-SDK production project dependency on a code-analysis package: analyzer is supplied by the existing Windows .NET tooling, not shipped to consumers.

## Alternatives and limits

An EditorConfig suggestion alone does not fail CI. Blind regex analysis of the entire C# language is not a semantic substitute for Roslyn, so the supplemental check only handles conventional single-line `const` declarations. A wholesale `dotnet format` rewrite or enabling every style analyzer simultaneously would conflate a focused naming fix with behavior-preserving modernization work and introduce unrelated diffs. Existing brace, qualification, whitespace and `var` policies still require review; they are **not** all automatically enforced by this ADR.

## Evidence

[PR #94](https://github.com/infoconex/RecordSift/pull/94) records Red and Green workflow evidence, including negative Roslyn constant naming and the supplementary uppercase-underscore fixture. Running the two independent NUnit assemblies confirms no behavior regressions.
