# C# coding conventions and library API standards

**Policy:** Engineering issue [#84](https://github.com/infoconex/RecordSift/issues/84). Follow Microsoft's published C# coding conventions and .NET Framework Design Guidelines, with the repository's root `.editorconfig` as the explicit local style baseline.

## Current target and compatibility

- **Target:** .NET Framework 4.8, conventional non-SDK Visual Studio projects, `LangVersion` **7.3**. Do not use newer language features (file-scoped namespaces, nullable-reference annotations, records, primary constructors, global usings, newer pattern forms) merely because a recent SDK supports them.
- Use `namespace RecordSift { ... }` / block-scoped namespaces, four-space indents, UTF-8, LF, one declaration per relevant file, and consistent braces for new/modified control-flow code.
- Use `using` directives outside the namespace, sort `System` first, eliminate unused imports and avoid wildcard imports. Do not change legacy files solely to satisfy cosmetic preferences outside the issue scope.

## Names, APIs and safety

- Public namespaces, types, methods, properties, events and enum members: **PascalCase**. Interfaces: `I` + PascalCase (e.g. `ICsvRecordReader`). Parameters and locals: **camelCase**. Private fields: `_camelCase` where instance fields are needed. Constants: PascalCase. Prefer meaningful domain names over implementation names.
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

## Enforcement strategy

The root `.editorconfig` is editor-level guidance for baseline C# style and interface naming. The existing traditional projects do **not** yet run a mandatory Roslyn analyzer/format gate; do not claim style is automated or enforced in CI. Add analyzers and baseline-aware gate in a separately approved engineering change, after evaluating version compatibility and pre-existing violations. Current mandatory observed gates remain the Windows net48 build/NUnit suite and documentation validator.

## Microsoft reference guides

- [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [C# identifier naming](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Interface design](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/interface)
- [EditorConfig in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options)
- [Architectural principles and dependency inversion](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

Repository-specific choices above take precedence where the official guidance offers multiple alternatives or illustrates modern C# features not available to C# 7.3.
