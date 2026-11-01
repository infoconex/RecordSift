# RecordSift {{PACKAGE_VERSION}}

**Import, validate and report on structured data.** RecordSift is an MIT-licensed library for **Windows and .NET Framework 4.8**. C# 7.3 is the project language baseline. It supports CSV and XLSX imports with typed schema mapping, validation, structured diagnostics and JSON, readable-text and machine-interchange CSV reports.

## Install and use

Install `RecordSift` version **{{PACKAGE_VERSION}}** from the intended NuGet source into a Windows `net48` consumer. The package contains five RecordSift assemblies (Domain, Application, Infrastructure.Csv, Infrastructure.Xlsx and Infrastructure.Reporting) and their XML documentation in `lib/net48`; NuGet installs their declared dependencies separately.

Public API examples and a detailed consumer guide are available in the [RecordSift repository](https://github.com/infoconex/RecordSift/blob/main/README.md) and [0.1 consumer guide](https://github.com/infoconex/RecordSift/blob/main/docs/release/0.1-consumer-guide.md). No Microsoft Office installation or external runtime service is required.

## Security and limitations

Imports are synchronous and may eagerly buffer records. Configure input byte, worksheet and regex limits conservatively when handling untrusted data. The XLSX adapter performs bounded ZIP expansion and XML/shared-string preflight; these controls do **not** guarantee a hard maximum process heap or immediate cancellation of expensive vendor-parser operations. Only Windows/.NET Framework 4.8 is supported. Use a separate process and additional resource controls where stronger isolation is required.

Diagnostic CSV is a lossless **machine-interchange** format, **not spreadsheet-formula-safe** output. Do not directly open exported diagnostics containing untrusted identifiers in Excel or similar applications; explicitly import affected columns as text. Review the [security assessment](https://github.com/infoconex/RecordSift/blob/main/docs/engineering/release-0.1-security-assessment.md) and [consumer guidance](https://github.com/infoconex/RecordSift/blob/main/docs/release/0.1-consumer-guide.md).

## Licensing and support

RecordSift itself is **MIT**, Copyright (c) 2026 Infoconex. The package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`. The pinned runtime dependencies include CsvHelper 33.1.0 and ExcelDataReader 3.9.0 and are independently licensed and installed by NuGet; their vendor notices and the full eleven-package inventory are available in the [dependency review](https://github.com/infoconex/RecordSift/blob/main/docs/release/0.1-dependencies.md).

Report defects and security-sensitive findings through the [repository's issue and responsible disclosure guidance](https://github.com/infoconex/RecordSift). No cross-platform, streaming, async or unbounded-resource safety promise is made for version {{PACKAGE_VERSION}}.
