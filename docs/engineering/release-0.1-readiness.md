# RecordSift release 0.1 — release-candidate readiness assessment

**Review:** 2026-10-31. **Supported scope:** Windows only; .NET Framework 4.8; C# 7.3; NUnit 3; GitHub Actions Windows 2022 runners. **Integration branch:** release/0.1. **Verified candidate integration SHA before this documentation PR:** b56642252c8824f6d4bdcd09e0704842a3352a23. Do not confuse this review with release authorization.

## Functional and nonfunctional acceptance

The prior functional-completion assessment, at SHA 7850680619cd1652182d50ac7485a59c55c233a8, accepted 25 Must and 8 targeted Should functional requirements in four workstreams (schema mapping/conversion, record validation, processing controls, diagnostic reporting). The 6 Future requirements remain deferred. All 10 NFRs were accepted for Windows/net48 only. The verified NUnit baseline is **379 passing cases**: 121 Domain, 62 Application, 122 CSV, 41 Reporting, 33 XLSX.

The compiled public API baseline, the seven JSON golden fixture checks (Completed/Failed and established Stopped/Canceled extensions), XML documentation, architecture and naming checks remained enforced throughout candidate validation. The two formerly implicit 0.0.0.0 adapter assembly/file versions were deliberately brought to 0.1.0.0 before first public distribution, and the approved baseline reflects those four identity lines. No other public contract evolution was introduced.

## Packaging and installed-consumer results

- Issue #183, PR #187: unpublished NuGet candidate packaging, versions and XML docs. Squash-merged at f853f65336fe1edd32a139af49174fa3accc41d2. PR Windows run #41236108409 and documentation run #41236108529 passed. **Post-merge** Windows run #41236358404 and docs run #41236358415 passed.
- Issue #184, PR #188: separate Windows C# 7.3 / net48 consumer, independent local NuGet installation, compile against installed RecordSift DLLs, CSV and XLSX processing, schema mapping, custom record validation, structured results and all three report formats. Squash-merged at b56642252c8824f6d4bdcd09e0704842a3352a23. Its finalized PR checks passed Windows #41236545229, docs #41236545231 and clean consumer #41236545234. **Post-merge integration** Windows #41236757023, docs #41236756970 and clean consumer #41236757085 all passed.
- Consumer verification exposed that a package declaring only two direct vendor dependencies installed older transitive minimum versions. The package now declares exact versions for the entire dependency inventory pinned by both vendor adapter projects. The independent install asserts each pinned package version. The clean Windows consumer logs confirm System.Memory 4.5.5, System.Buffers 4.5.1, System.Runtime.CompilerServices.Unsafe 6.0.0, CsvHelper 33.1.0, ExcelDataReader 3.9.0 and the remaining declared versions.
- The nupkg contains exactly five RecordSift net48 DLLs and corresponding XML files plus package README; vendor binaries are installed by NuGet rather than repackaged as project DLLs. Candidate 0.1.0-rc.1 is created locally, never uploaded. A repeatable packaging procedure and SHA256 evidence exist, but **byte-for-byte reproducible ZIP hashes across machines were not established**, in part because NuGet ZIP timestamps can vary.

## Third-party security, licensing, and operational review

The accepted Windows jobs include pinned-manifest verification and NuGet advisory audits that fail on NU1900–NU1905 warnings, plus Office-free tests, no-service runtime scenarios, bounded resource controls, public API and XML docs checks. Windows #41236757023 and clean consumer #41236757085 were successful on the latest verified integration SHA. These checks do not promise that future vulnerabilities will never be disclosed; rerun the audit before release promotion. Review of release scripts, test fixture data and versioned documentation found no intentionally embedded credentials. This review is not a comprehensive third-party secret-scanner certification. Owner signoff should cover the final repository contents and package-distribution notices.

Vendor license evidence was subsequently completed in PR #190 for all eleven pinned runtime packages: CsvHelper 33.1.0 uses MS-PL OR Apache-2.0; ExcelDataReader and the nine Microsoft runtime packages use MIT (older versions link the MIT CoreFX terms). The owner then approved MIT licensing for RecordSift-owned source with copyright (c) 2026 Infoconex, issue #186. The repository now contains root LICENSE, third-party notices, SPDX MIT package metadata, and package/installed-consumer verification of those files. License grant conditions still apply to consumers and to any separately redistributed third-party dependencies.

## Documentation and release operations

Issue #185 / PR #189 updates the root README, net48 installation guide and examples, complete release notes, versioned JSON/report behavior and limitations, pinned package inventory, licensing warnings, and release verification/promotion/rollback procedures. Windows #41236650556 and documentation #41236650553 passed prior to the final assessment revision. The documentation PR must undergo fresh verification and post-merge CI after this report is added.

No merge to main, public tag, GitHub release or package publication occurred. The release/0.1 integration branch remains the only changed integration branch. Independent authorization is mandatory for any subsequent promotion.

## Licensing acceptance update — 2026-10-31

After the prior technical readiness report, PR #190 was squash-merged to `release/0.1` at `236b274b063bd049d5c6467e33a8e673bab9ffc3`. Post-merge Windows [#41239476241](https://github.com/infoconex/RecordSift/actions/runs/41239476241) passed 379 NUnit tests, package validation and dependency audit; independent consumer [#41239476254](https://github.com/infoconex/RecordSift/actions/runs/41239476254) and docs [#41239476293](https://github.com/infoconex/RecordSift/actions/runs/41239476293) passed.

The owner explicitly approved MIT licensing, **Copyright (c) 2026 Infoconex**, in the development conversation on 2026-10-31. The licensing workstream adds the exact approved LICENSE, `<license type="expression">MIT</license>` NuGet metadata, version-pinned third-party notices in the package and validation scripts that reject missing or modified notices and non-MIT license expressions. **Licensing acceptance is verified:** PR #191 squash-merged to `release/0.1` at `2332d9ed42568b6e6e554f3e304a675bc00de513` and post-merge Windows [#41240577928](https://github.com/infoconex/RecordSift/actions/runs/41240577928) passed all 379 NUnit tests and the 425-entry compiled API compatibility gate; independent installed consumer [#41240577943](https://github.com/infoconex/RecordSift/actions/runs/41240577943) and documentation [#41240577944](https://github.com/infoconex/RecordSift/actions/runs/41240577944) passed. Issue #186 is closed. No public release has been authorized.

## Internal stable-package preflight — issue #192

The previously tested unpublished `0.1.0-rc.1` candidate is retained. Issue [#192](https://github.com/infoconex/RecordSift/issues/192) adds an internal-only, explicitly selected stable `RecordSift.0.1.0.nupkg` build. The same Windows package builder verifies the effective packed NuGet version (not just the source nuspec), MIT license and notices, five net48 assemblies and documentation, and 0.1.0.0 assembly identities. The separate installed-consumer workflow exercises **both** RC and stable version identities from independent temporary local feeds with real CSV/XLSX import, validation and reporting. The full build, NUnit, API/JSON, audit, docs and style controls remain separate gates.

The workflow produces **temporary local artifacts only**. It does not upload them, publish a GitHub release, tag a commit, promote to `main` or grant authorization to ship. Approval for external distribution and a final checksum are still required **after** these preflight acceptance checks and a verification run on the precise promoted release commit.

## Risks and classification

**RECOMMENDATION: CONDITIONAL GO — technically validated unpublished release candidate, NOT yet authorized for release promotion or publication.**

There are no *known unresolved release-blocking implementation defects* in the scope that passed the Windows candidate and installed-consumer gates. The candidate must **not** be publicly distributed or promoted until all of the following explicit conditions are satisfied:

1. **Verified complete:** the owner approved MIT, copyright (c) 2026 Infoconex (#186); root LICENSE, SPDX MIT metadata and third-party notices are committed.
2. Verify the unpublished stable `0.1.0` preflight package and independent installed consumer in Windows CI (#192). **Even if local preflight passes**, rerun Windows regression, installed-consumer, API/JSON/documentation, and fresh vulnerability checks on the **exact promoted final artifact** before publication.
3. A final version, checksum, versioned release notes, dependency notices and promotion/rollback plan are reviewed. Release/0.1 is promoted to main and a public release/tag/package are created **only after separate explicit authorization**.

If the license cannot be approved, CI fails on the final package or a new release-blocking defect is discovered, the decision becomes NO-GO until remediated. The six deferred Future functional requirements and cross-platform modernization are outside the approved Windows 0.1 scope, not blockers.

**Decision owner:** repository/release owner; this technical assessment does not authorize or execute promotion.
