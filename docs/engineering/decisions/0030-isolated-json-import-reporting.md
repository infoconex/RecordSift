# ADR 0030 — Format-neutral JSON report serialization adapter

**Status:** Accepted for release 0.1  
**Issue:** [#36](https://github.com/infoconex/RecordSift/issues/36)  
**Requirement:** [FR-035](../../scenarios/functional/FR-035.md)

## Decision

Application owns the format-neutral `IImportResultSerializer` port accepting a completed `TypedImportResult<T>`. A separate `RecordSift.Infrastructure.Reporting` assembly implements `JsonImportResultSerializerAdapter`, using the .NET Framework 4.8 `DataContractJsonSerializer` and explicit version 1.0 wire-contract data transfer objects. No JSON or parser dependency is introduced into Domain or Application, and no NuGet serializer package is necessary.

The report is a projection, not a second business model: all statuses, counts, and diagnostics originate from existing Domain result invariants. The JSON top-level properties are `schemaVersion`, `sourceId`, `status`, `message`, `summary`, `diagnostics`, in stable order. The summary exposes three row counts only. Every diagnostic includes `scope`, nullable `row`, nullable `field`, `code`, and sanitized `message`. Optional location fields appear only when known. Diagnostic order is retained; accepted record values, warning severities, stack traces, and vendor exceptions are not exported.

The default JSON report does **not** repeat the caller-controlled `TypedImportResult<T>.Message`, which may contain private input. It selects a safe, invariant status message from the Reporting adapter's resource catalog based only on Domain status and summary counts. Opaque non-path `sourceId` values are preserved; absolute Windows, POSIX, UNC, or file-URI source identifiers are replaced with the stable `source-redacted` identifier. This privacy policy applies even to manually constructed results. The report never infers or includes raw accepted records. Detailed sanitized diagnostic reasons remain available through Domain diagnostic codes and messages.

## Consequences

The JSON contract is independently versioned as `1.0`; incompatible output changes require deliberate schema-version evolution. The Infrastructure reporting adapter can be replaced without changing Domain rules or Application importers. It can serialize results produced by future XLSX workflows. NUnit acceptance verifies parsed JSON shape and semantic invariants, not whitespace-based snapshots. Dedicated Windows CI runs the reporting assembly's tests independently.
