# Nonfunctional requirements — RecordSift

**Baseline:** .NET Framework 4.8 / C# 7.3 release 0.1. Nonfunctional scenario verification is tracked individually in [scenario-to-test traceability](../testing/traceability.md), including observed Windows CI. Completed net48 acceptance does **not** establish cross-platform compatibility, a framework migration comparison, or package publication.

| Requirement | Quality attribute | Intent | GitHub issue / acceptance |
| --- | --- | --- | --- |
| NFR-001 | Straightforward developer API | Release 0.1 quality gate | [#41](https://github.com/infoconex/RecordSift/issues/41) (`NFR-001-SC01`) · [All scenarios](../scenarios/non-functional/NFR-001.md) |
| NFR-002 | No required external services | Release 0.1 quality gate | [#42](https://github.com/infoconex/RecordSift/issues/42) (`NFR-002-SC01`) · [All scenarios](../scenarios/non-functional/NFR-002.md) |
| NFR-003 | No Microsoft Excel installation requirement | Release 0.1 quality gate | [#43](https://github.com/infoconex/RecordSift/issues/43) (`NFR-003-SC01`) · [All scenarios](../scenarios/non-functional/NFR-003.md) |
| NFR-004 | Deterministic validation | Release 0.1 quality gate | [#44](https://github.com/infoconex/RecordSift/issues/44) (`NFR-004-SC01`) · [All scenarios](../scenarios/non-functional/NFR-004.md) |
| NFR-005 | Configurable culture-sensitive parsing | Release 0.1 quality gate | [#45](https://github.com/infoconex/RecordSift/issues/45) (`NFR-005-SC01`) · [All scenarios](../scenarios/non-functional/NFR-005.md) |
| NFR-006 | Actionable malformed-input failures | Release 0.1 quality gate | [#46](https://github.com/infoconex/RecordSift/issues/46) (`NFR-006-SC01`) · [All scenarios](../scenarios/non-functional/NFR-006.md) |
| NFR-007 | Resource limits | Release 0.1 quality gate | [#47](https://github.com/infoconex/RecordSift/issues/47) (`NFR-007-SC01`) · [All scenarios](../scenarios/non-functional/NFR-007.md) |
| NFR-008 | No execution of macros or input code | Release 0.1 quality gate | [#48](https://github.com/infoconex/RecordSift/issues/48) (`NFR-008-SC01`) · [All scenarios](../scenarios/non-functional/NFR-008.md) |
| NFR-009 | Avoid unnecessary OS-specific components | Release 0.1 quality gate | [#49](https://github.com/infoconex/RecordSift/issues/49) (`NFR-009-SC01`) · [All scenarios](../scenarios/non-functional/NFR-009.md) |
| NFR-010 | Observable public API compatibility | Release 0.1 quality gate | [#50](https://github.com/infoconex/RecordSift/issues/50) (`NFR-010-SC01`) · [All scenarios](../scenarios/non-functional/NFR-010.md) |

## Verification policy

- Each issue is individually tracked and cannot be closed merely because another feature was implemented.
- Tests must cover functional and nonfunctional behavior where executable evidence is possible; design reviews supplement, not replace, testable guarantees.
- Establish quantitative defaults and measurements (limits, benchmark budgets, acceptable variation) before treating performance/resource assertions as verified.
- Source files and error reports may contain sensitive data. Avoid leaking source values in logs and diagnostics by default.
- Dependencies must undergo compatibility, licensing, and vulnerability checks as part of the release process.
- `.NET Framework 4.8` is the **first historical-style baseline**. Cross-platform support and later `net8.0` / newer SDK experiments are separate milestones, not claims of release 0.1.
- No Microsoft Excel installation or external network/service should be required for the core import API.
