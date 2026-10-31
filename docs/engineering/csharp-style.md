# C# coding conventions and library API standards

**Policy:** Engineering issue [#84](https://github.com/infoconex/RecordSift/issues/84). Follow Microsoft's published C# coding conventions and .NET Framework Design Guidelines, with the repository's root `.editorconfig` as the explicit local style baseline.

## Current target and compatibility

- **Target:** .NET Framework 4.8, conventional non-SDK Visual Studio projects, `LangVersion` **7.3**. Do not use newer language features (file-scoped namespaces, nullable-reference annotations, records, primary constructors, global usings, newer pattern forms) merely because a recent SDK supports them.
- Use block-scoped, assembly/responsibility-aligned namespaces (`RecordSift.Domain.Diagnostics`, `RecordSift.Domain.Schema`, `RecordSift.Domain.Results`, `RecordSift.Application.Ports.Csv`, `RecordSift.Application.Importing.Csv`, or `RecordSift.Infrastructure.Csv`), four-space indents, UTF-8, LF, one declaration per relevant file, and consistent braces for new/modified control-flow code.
- Use `using` directives outside the namespace, sort `System` first, eliminate unused imports and avoid wildcard imports. Do not change legacy files solely to satisfy cosmetic preferences outside the issue scope.

## Names, APIs and safety

- Public namespaces must match the owning layer and responsibility; flat `namespace RecordSift` declarations are prohibited by CI. Public types, methods, properties, events and enum members: **PascalCase**. Interfaces: `I` + PascalCase (e.g. `ICsvRecordReader`). Parameters and locals: **camelCase**. Private nonconstant instance fields: `_camelCase`; private static nonconstant fields: `s_camelCase`. All `const` identifiers (public/private/local) are PascalCase **without underscores**. The default diagnostic string values such as `SOURCE_UNREADABLE` are stable external data, not C# names. Prefer meaningful domain names over implementation names.
- Method names express actions; properties represent state. Prefer explicit guards and `nameof(parameter)` for argument exceptions. Document externally observable behavior and ownership of input readers/streams.
- Preserve public API compatibility; do not expose vendor types or leak unsafe exception text in diagnostics. Follow standard `IDisposable` ownership rules for resources the implementation owns.
- Prefer `var` only when the type is readily apparent; otherwise declare the type. Favor explicit culture/encoding and immutable or read-only views of returned data.
- Avoid hidden state, locale-dependent behavior, broad exception swallowing, and logging raw imported source values.
- Favor focused, testable functions, dependency injection at third-party boundaries, and small interfaces with precisely specified semantics. Do not refactor working code into unnecessary abstractions.

## Domain messages and meaningful constants

- Stable diagnostic identifiers are **nonlocalized core constants**. Their invariant English text lives in `src/RecordSift.Domain/Diagnostics/Resources/DomainMessages.resx` and is retrieved through a core-owned resource accessor. Never include source data, secrets, file paths or vendor exception messages.
- Argument-validation strings are resource-backed through named properties in their **owning layer**: `DomainMessageResources.PositionMappingDuplicate` for Domain schema rules and `CsvMessageResources.ReaderHeaderConflict` for Application CSV rules. Raw `Argument_*` keys are confined to each catalog; `Get(string resourceKey)` remains internal. Consumers rely on exception types and parameter names, not English text. See [ADR 0016](decisions/0016-typed-domain-resource-messages.md) and [ADR 0020](decisions/0020-application-csv-message-ownership.md).
- Keep options and defaults in the core, e.g. `CsvReadOptions.Default`. Third-party adapters must implement that contract.
- Do not introduce constants for ordinary zero/one indices or CSV protocol syntax solely to eliminate literals; eliminate unexplained, duplicated, or variable business values.
- `scripts/validate_core_messages.py` validates both embedded catalogs and flags English prose in Domain/Application C# source. Issue #91 separately enforces third-party-free domain unit tests.

## Enforced naming checks and remaining coverage

The root `.editorconfig` specifies the Microsoft naming conventions (PascalCase constants, interfaces beginning with `I`, PascalCase types/members, camelCase locals and parameters, `_camelCase` private instance fields, `s_camelCase` private static fields). The Windows CI build runs the **Microsoft Roslyn IDE1006** naming analyzer through `dotnet format style` and fails on naming diagnostics. This works with the historical non-SDK net48 solution and does not retarget or add runtime dependencies.

```powershell
nuget restore RecordSift.sln -NonInteractive -PackagesDirectory packages -ConfigFile NuGet.Config
dotnet format style RecordSift.sln --no-restore --verify-no-changes --diagnostics IDE1006 --severity error --verbosity normal
python scripts/validate_constant_names.py
```

Microsoft's Roslyn naming analyzer does not consistently reject uppercase `UPPER_SNAKE_CASE` constant declarations, so the standard-library Python const-declaration guard **supplements** IDE1006. It validates positive and negative examples on every documentation CI run and inspects C# production and test sources, without loading any parser vendor package. An actual analyzer Red test is preserved in [issue #93](https://github.com/infoconex/RecordSift/issues/93). Separate test method identifiers intentionally include immutable `FR_###_SC##` / `NFR_###_SC##` traceability labels; the broad member-naming rule targets production code, while constant/parameter/interface rules apply to all source and tests.

This is **mandatory naming enforcement**, not proof that every Microsoft formatting and design recommendation has been automated. Existing code-style settings for braces, `var`, and qualification remain review guidance; a wider formatting/analyzer baseline should be introduced as independent work, not mixed into a non-behavioral naming migration. See [ADR 0012](decisions/0012-microsoft-csharp-naming-ci.md).

## Public API XML documentation and build enforcement

Engineering [#152](https://github.com/infoconex/RecordSift/issues/152) requires meaningful XML comments for **externally visible** types, constructors, members, enum values and relevant generic parameters across all five production assemblies. Document consumer-visible invariants, defaults, valid ranges, read-only snapshots, row/source coordinates, caller-owned streams/readers, structured diagnostics versus thrown programmer errors, and security boundaries. Do not claim atomic or partial results without checking the implementation and tests. Use `<summary>`, `<param>`, `<typeparam>`, `<returns>`, `<exception>`, `<remarks>`, `<see>`, or `<paramref>` where they add precision. A useful comment explains behavior and consequences, not merely the member name. For interfaces and implementations, use concise independent documentation so conventional non-SDK compiler XML output remains useful in IntelliSense.

Conventional C# 7.3/.NET Framework 4.8 project files generate `bin\\Debug\\RecordSift.*.xml` and `bin\\Release\\RecordSift.*.xml`, adjacent to their assemblies. **CS1591 is elevated to an error**, without broad warning suppression, in both build configurations. Windows CI validates each Release XML output and runs a negative MSBuild probe that removes one required member comment and requires a CS1591 failure. Do not commit generated XML files or remove documentation checks. Internal implementation comments should explain non-obvious decisions, threat boundaries and tradeoffs; keep architecture history in ADRs and requirements in versioned specifications.

## Microsoft reference guides

- [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [C# identifier naming](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Interface design](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/interface)
- [EditorConfig in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options)
- [Architectural principles and dependency inversion](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

Repository-specific choices above take precedence where the official guidance offers multiple alternatives or illustrates modern C# features not available to C# 7.3.
