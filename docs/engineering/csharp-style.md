# C# coding standards

Follow Microsoft's [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions), [identifier naming guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names), and [.NET Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/).

The rules below capture RecordSift-specific choices and exceptions. They apply to new or changed code; do not reformat unrelated working code solely for style. The root [`.editorconfig`](../../.editorconfig) represents supported settings, not the entire coding standard.

## Compatibility

- Target .NET Framework 4.8.
- Use C# 7.3-compatible syntax.
- Use block-scoped namespaces.
- Do not adopt newer language features merely because a recent SDK supports them.
- Preserve traditional project files until a separately approved project-format change.

## Formatting and language usage

- Indent C# with four spaces.
- Use UTF-8 encoding.
- Use LF line endings.
- Place `using` directives outside namespaces.
- Place `System` directives before other imports.
- Remove unused `using` directives.
- Avoid wildcard imports.
- Use braces for control-flow statements.
- Prefer one focused type declaration per file.
- Use `var` when the inferred type is clear.
- Use an explicit type when inference would obscure meaning.

## Naming and namespaces

- Use PascalCase for types.
- Use PascalCase for methods.
- Use PascalCase for properties.
- Use PascalCase for events.
- Use PascalCase for enum members.
- Prefix interface names with `I` and use PascalCase.
- Use camelCase for parameters.
- Use camelCase for local variables.
- Use `_camelCase` for private instance fields.
- Use `s_camelCase` for private static nonconstant fields.
- Use PascalCase without underscores for all `const` identifiers.
- Align namespaces with the owning layer and responsibility.
- Do not use the flat `RecordSift` namespace in production code.
- Prefer meaningful domain names over implementation-oriented names.
- Preserve externally visible diagnostic string values when renaming C# identifiers.

## API design and implementation

- Name methods for actions.
- Name properties for state.
- Validate invalid arguments with explicit guards.
- Use `nameof(parameter)` for argument-exception parameter names.
- Preserve existing public API contracts.
- Keep third-party types out of public and core contracts.
- Keep vendor exception text out of diagnostics.
- Document ownership of caller-provided readers and streams.
- Follow standard `IDisposable` ownership conventions.
- Use explicit culture where parsing or formatting depends on culture.
- Use explicit encoding where text interpretation depends on encoding.
- Prefer immutable or read-only views for returned data.
- Avoid hidden global state.
- Avoid swallowing exceptions broadly.
- Do not log untrusted source values or secrets.
- Prefer focused, testable functions.
- Inject dependencies at third-party boundaries.
- Keep interfaces small and their contracts precise.
- Do not introduce abstractions without a clear benefit.

See the [architecture guidance](architecture.md) for dependency-direction and adapter requirements.

## C# examples in public documentation

- Name instantiated objects after their type in camelCase (for example, `csvImporter`).
- Name returned values for their actual type or role (for example, `typedImportResult`).
- Use descriptive resource names such as `stringReader` or `fileStream`.
- Avoid ambiguous names such as `importer`, `mapper`, or `result` in new examples.
- Keep examples compatible with C# 7.3.
- Verify examples against actual public API signatures.
- Use named, positive values for required CSV `maxCharacters` and XLSX `maxBytes` limits.
- Do not imply that mandatory input-size arguments have defaults.
- Do not rename existing production identifiers solely to match documentation examples.

## Messages and constants

- Keep stable diagnostic codes invariant and nonlocalized.
- Store Domain diagnostic messages in `DomainMessages.resx`.
- Store Application-owned CSV argument messages in `CsvMessages.resx`.
- Access messages through typed, owning-layer resource properties.
- Keep raw `Argument_*` resource keys within their owning catalog.
- Keep raw resource-key lookup methods internal.
- Keep source content, secrets, paths, and vendor exceptions out of diagnostics.
- Keep options and defaults in the core contracts.
- Require adapters to honor core defaults.
- Do not create constants for ordinary zero/one indices or CSV syntax alone.
- Replace unexplained or duplicated business literals with meaningful names.

See [ADR 0016](decisions/0016-typed-domain-resource-messages.md) and [ADR 0020](decisions/0020-application-csv-message-ownership.md) for resource ownership.

## XML documentation

- Document externally visible APIs with meaningful XML comments.
- Document observable behavior, input constraints, defaults, and failure conditions.
- Document ownership and disposal responsibilities for readers and streams.
- Explain security boundaries and important invariants where relevant.
- Use XML tags such as `<summary>`, `<param>`, and `<exception>` when useful.
- Do not write comments that merely restate member names.
- Explain non-obvious implementation decisions in internal comments.
- Keep architecture history in ADRs and behavior requirements in specifications.
- Do not commit generated XML documentation output.

## Verification

- Treat compiler warning `CS1591` as an error in Debug and Release production builds.
- Enforce selected naming rules with Roslyn `IDE1006` in Windows CI.
- Supplement Roslyn with the focused uppercase-underscore constant-name check.
- Preserve `FR_###_SC##` and `NFR_###_SC##` test-method identifiers.
- Keep broader formatting, `var`, and qualification preferences as editor and review guidance.
- Do not claim that the naming gate enforces every Microsoft coding convention.

To reproduce the current naming checks after restoring packages:

```powershell
nuget restore RecordSift.sln -NonInteractive -PackagesDirectory packages -ConfigFile NuGet.Config
dotnet format style RecordSift.sln --no-restore --verify-no-changes --diagnostics IDE1006 --severity error --verbosity normal
python scripts/validate_constant_names.py
```

Windows CI also verifies generated XML documentation and tests that an undocumented public member triggers `CS1591`.

See [ADR 0012](decisions/0012-microsoft-csharp-naming-ci.md) for naming-rule enforcement and its known limits. See [issue #152](https://github.com/infoconex/RecordSift/issues/152) for public XML documentation requirements.
