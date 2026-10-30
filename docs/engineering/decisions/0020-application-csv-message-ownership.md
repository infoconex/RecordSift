# ADR 0020 — Assign CSV validation messages to Application resources

**Status:** Accepted through [engineering issue #109](https://github.com/infoconex/RecordSift/issues/109). Refines [ADR 0016](0016-typed-domain-resource-messages.md) and preserves its pre-release resource-backed message contract.

## Context

Domain owns business schema, diagnostic codes, source/record diagnostic text, and import result invariants. Five messages in Domain's original `DomainMessages.resx` instead described technical preconditions of the Application CSV reader and table orchestration. This was not a forbidden assembly reference, but it made Domain aware of higher-level CSV workflow vocabulary.

## Decision

Move exactly these five resource keys and corresponding **PascalCase** named string accessors to the Application assembly:

| Resource key | Application validation owner |
| --- | --- |
| `Argument_ReaderHeaderConflict` | `CsvTableReader` headed binding |
| `Argument_HeaderlessMissingPositions` | `CsvTableReader` headerless binding |
| `Argument_FailedTableNoError` | `CsvTableResult` failure invariants |
| `Argument_FailedTableNonSourceError` | `CsvTableResult` source-scoped failures |
| `Argument_ParserErrorScope` | `CsvParseResult` source-scoped parsing failures |

The Domain catalog continues to own **11** stable diagnostic codes/messages and **19** Domain argument messages. The Application catalog has exactly **5** CSV argument messages in `src/RecordSift.Application/Messages/Resources/CsvMessages.resx`, embedded with logical identity `RecordSift.Resources.CsvMessages.resources`. The accessor is `RecordSift.Application.Messages.CsvMessageResources`; it exposes five typed named properties but keeps raw `Get(string)` internal.

Application callers use their own named properties. Infrastructure continues to receive source errors as Domain-owned codes and diagnostics, independent of CsvHelper details. No new Domain dependency on Application exists, and no third-party package reference is introduced.

## Compatibility and verification

This is an intentional pre-release API removal of five previously public `DomainMessageResources` properties; consumers must use `CsvMessageResources` for those five technical precondition messages. All **English text**, exception types and parameter names, diagnostic identifiers and invariant culture handling remain byte-for-byte unchanged. Raw resource strings are not embedded in production callers. Domain and Application resources are disjoint, with automated exact key/accessor and manifest assertions.

CI verifies the Domain/Application project graph, independent message contracts in each NUnit layer, safe diagnostic translation against the real CSV adapter, Roslyn naming, Windows net48 compilation, and red/green resource ownership tests. Historical behavior and FR/NFR acceptance statuses are unchanged.

## Alternatives rejected

Keeping CSV-specific messages in Domain would leak Application concerns into business resources. Duplicating them in both catalogs weakens single ownership. Moving Domain source diagnostics into Application would break the stable Domain-owned diagnostic contract. Adding a generic resource helper or introducing localization services is unnecessary for this focused refactor.
