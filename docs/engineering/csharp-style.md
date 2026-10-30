# C# coding conventions and library API standards

**Policy:** Engineering issue [#84](https://github.com/infoconex/RecordSift/issues/84). Follow Microsoft's published C# coding conventions and .NET Framework Design Guidelines, with the repository's root `.editorconfig` as the explicit local style baseline.

## Current target and compatibility

- **Target:** .NET Framework 4.8, conventional non-SDK Visual Studio projects, `LangVersion` **7.3**. Do not use newer language features (file-scoped namespaces, nullable-reference annotations, records, primary constructors, global usings, newer pattern forms) merely because a recent SDK supports them.
- Use `namespace RecordSift { ... }` / block-scoped namespaces, four-space indents, UTF-8, LF, one declaration per relevant file, and consistent braces for new/modified control-flow code.
- Use `using` directives outside the namespace, sort `System` first, eliminate unused imports and avoid wildcard imports. Do not change legacy files solely to satisfy cosmetic preferences outside the issue scope.

## Names, APIs and safety

- Public namespaces, types, methods, properties, events and enum members: **PascalCase**. Interfaces: `I` + PascalCase (e.g. `ICsvRecordReader`). Parameters and locals: **camelCase**. Private nonconstant instance fields: `_camelCase`; private static nonconstant fields: `s_camelCase`. All `const` identifiers (public/private/local) are PascalCase **without underscores**. The default diagnostic string values such as `SOURCE_UNREADABLE` are stable external data, not C# names. Prefer meaningful domain names over implementation names.
- Method names express actions; properties represent state. Prefer explicit guards and `nameof(parameter)` for argument exceptions. Document externally observable behavior and ownership of input readers/streams.
- Preserve public API compatibility; do not expose vendor types or leak unsafe exception text in diagnostics. Follow standard `IDisposable` ownership rules for resources the implementation owns.
- Prefer `var` only when the type is readily apparent; otherwise declare the type. Favor explicit culture/encoding and immutable or read-only views of returned data.
- Avoid hidden state, locale-dependent behavior, broad exception swallowing, and logging raw imported source values.
- Favor focused, testable functions, dependency injection at third-party boundaries, and small interfaces with precisely specified semantics. Do not refactor working code into unnecessary abstractions.

## Domain messages and meaningful constants

- Stable diagnostic identifiers are **nonlocalized core constants**. Their invariant English text lives in `src/RecordSift/Resources/DomainMessages.resx` and is retrieved through a core-owned resource accessor. Never include source data, secrets, file paths or vendor exception messages.
- Argument-validation strings are resource-backed; consumers should rely on exception types and parameter names, not the English text.
- Keep options and defaults in the core, e.g. `CsvReadOptions.Default`. Third-party adapters must implement that contract.
- Do not introduce constants for ordinary zero/one indices or CSV protocol syntax solely to eliminate literals; eliminate unexplained, duplicated, or variable business values.
- `scripts/validate_core_messages.py` validates embedded resources and flags English prose in core C# source. Issue #91 separately enforces third-party-free domain unit tests.

## Enforced naming checks and remaining coverage

The root `.editorconfig` specifies the Microsoft naming conventions (PascalCase constants, interfaces beginning with `I`, PascalCase types/members, camelCase locals and parameters, `_camelCase` private instance fields, `s_camelCase` private static fields). The Windows CI build runs the **Microsoft Roslyn IDE1006** naming analyzer through `dotnet format style` and fails on naming diagnostics. This works with the historical non-SDK net48 solution and does not retarget or add runtime dependencies.

```powershell
nuget restore RecordSift.sln -NonInteractive -PackagesDirectory packages -ConfigFile NuGet.Config
dotnet format style RecordSift.sln --no-restore --verify-no-changes --diagnostics IDE1006 --severity error --verbosity normal
python scripts/validate_constant_names.py
```

Microsoft's Roslyn naming analyzer does not consistently reject uppercase `UPPER_SNAKE_CASE` constant declarations, so the standard-library Python const-declaration guard **supplements** IDE1006. It validates positive and negative examples on every documentation CI run and inspects C# production and test sources, without loading any parser vendor package. An actual analyzer Red test is preserved in [issue #93](https://github.com/infoconex/RecordSift/issues/93). Separate test method identifiers intentionally include immutable `FR_###_SC##` / `NFR_###_SC##` traceability labels; the broad member-naming rule targets production code, while constant/parameter/interface rules apply to all source and tests.

This is **mandatory naming enforcement**, not proof that every Microsoft formatting and design recommendation has been automated. Existing code-style settings for braces, `var`, and qualification remain review guidance; a wider formatting/analyzer baseline should be introduced as independent work, not mixed into a non-behavioral naming migration. See [ADR 0012](decisions/0012-microsoft-csharp-naming-ci.md).

## Microsoft reference guides

- [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [C# identifier naming](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Interface design](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/interface)
- [EditorConfig in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options)
- [Architectural principles and dependency inversion](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

Repository-specific choices above take precedence where the official guidance offers multiple alternatives or illustrates modern C# features not available to C# 7.3.
