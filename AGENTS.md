# Agent instructions — RecordSift

These repository-wide instructions apply to human and AI-assisted contributions.

## Scope and branch discipline

- Read `docs/README.md` and the issue before editing.
- One distinct FR/NFR or engineering issue per working branch. Branch from the latest `release/0.1` using `issue/<number>-<slug>`.
- Target every issue PR to `release/0.1`; squash merge after evidence and review. Promote to `main` only through a release PR.
- Do not build unrelated features into an issue. Cross-reference dependencies; do not mark another requirement complete without its own evidence.
- Preserve stable `FR-###`, `NFR-###`, and `*-SC##` identifiers. Read and maintain `docs/scenarios/functional/FR-###.md` or `docs/scenarios/non-functional/NFR-###.md` as the authoritative behavior definition; update `docs/testing/traceability.md` on implementation PRs.

## Engineering and testing

- Follow [domain-centric architecture](docs/engineering/architecture.md), [ADR 0007](docs/engineering/decisions/0007-domain-ports-and-adapters.md), [Microsoft C# style](docs/engineering/csharp-style.md), and the root `.editorconfig`. Keep new code compatible with net48 / C# 7.3. Follow [ADR 0013](docs/engineering/decisions/0013-physical-domain-application-infrastructure-assemblies.md): `RecordSift.Domain` has no project dependencies, `RecordSift.Application` depends on Domain, and each infrastructure adapter depends inward on owned contracts. Place every new file in its corresponding layer; `scripts/validate_architecture_layers.py` enforces this in CI. Name format infrastructure projects `RecordSift.Infrastructure.<Format>`; name concrete implementations `<Role>Adapter` (e.g. `CsvRecordReaderAdapter` implementing `ICsvRecordReader`). Do not introduce `Helper` in RecordSift-owned public class, namespace, or assembly identifiers; the external CsvHelper package name is not ours to rename. See [ADR 0014](docs/engineering/decisions/0014-infrastructure-csv-adapter-naming.md).
- Apply SOLID pragmatically: domain rules and import orchestration depend on RecordSift-owned contracts; third-party readers live behind injected ports in adapter assemblies. No vendor types in domain, public result/diagnostic contracts, or application policy. Test port consumers with fakes and implementations with real adapter tests.
- Record package selection, licensing, vulnerabilities, pinned dependency versions and any public API migration in a dedicated issue/ADR. The CSV adapter migration is tracked in [#85](https://github.com/infoconex/RecordSift/issues/85) and [ADR 0008](docs/engineering/decisions/0008-csvhelper-isolated-adapter.md); do not claim full typed CSV acceptance from adapter tests.

- Naming is enforced (not merely suggested): all C# `const` names must be PascalCase, private instance fields `_camelCase`, private static nonconstant fields `s_camelCase`, public types/members PascalCase, interfaces `I` + PascalCase, locals/parameters camelCase. CI runs Roslyn IDE1006 and a targeted uppercase-snake-case constant guard; do not suppress the checks or change external diagnostic string values. See [ADR 0012](docs/engineering/decisions/0012-microsoft-csharp-naming-ci.md) and engineering [#93](https://github.com/infoconex/RecordSift/issues/93).
- Follow `docs/testing/strategy.md`: Red → Green → Refactor. Give every feature positive, negative and boundary test cases as appropriate.
- The domain/core defines error codes, resource-backed safe messages, source contracts, neutral options, and observable failure semantics. Adapters translate vendor outcomes to owned contracts; never leak vendor types or messages. Test the same contracts against independent fakes and real adapters. Core tests (`tests/RecordSift.Tests`) must **never** reference adapters or parser packages; adapter tests inherit reusable core-owned `CsvRecordReaderContractTests` and run separately with the real vendor implementation. See [#91](https://github.com/infoconex/RecordSift/issues/91).
- Never hardcode English messages in production C#; use domain-owned `src/RecordSift.Domain/Diagnostics/Resources/DomainMessages.resx` via `DomainMessageResources`. Diagnostic identifiers remain invariant and messages use invariant culture for deterministic output. Run `scripts/validate_core_messages.py` and never duplicate configuration defaults in adapters.
- Keep parsing, mapping, validation, and reporting behavior deterministic. Avoid ambient culture and hidden global state.
- Security: treat imported files as hostile, cap consumption, avoid logging raw data and secrets, do not evaluate spreadsheet macros/formulas.
- Use established packages only after reviewing compatibility, licensing and security; pin reproducible versions.
- Do **not** claim compilation, passing tests, security scans, performance improvements or external availability without observed evidence.
- Do not silently change public APIs or JSON contracts; add characterization tests and document intentional changes.

## Historical baseline and modernization

- The initial .NET Framework 4.8 non-SDK project is deliberately constructed for teaching; never describe it as recovered historical production code.
- For baseline issues, do not add SDK-style project files, Central Package Management or unexpected target frameworks.
- Keep project-format changes, SDK changes, dependency updates, target-framework migration, and code behavior changes independently measurable.
- `net8.0` with the .NET 10 SDK is a **later experiment**, not a baseline requirement.

## Review requirements

In every PR include: primary issue, scenario IDs, scope, test evidence/limitations, dependencies, and relevant documentation updates. If a local build environment is unavailable, state that explicitly and rely on a CI run before marking execution complete.
