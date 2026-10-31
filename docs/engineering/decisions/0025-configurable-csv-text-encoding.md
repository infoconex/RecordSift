# ADR 0025 — Explicit CSV byte-stream text decoding

**Decision:** Accept an explicit .NET Framework `Encoding` for byte-source CSV imports in [FR-006](../../scenarios/functional/FR-006.md), tracked by [issue #3](https://github.com/infoconex/RecordSift/issues/3). Keep byte decoding outside Domain and separate from CSV syntax parsing.

## Architectural boundary

- Existing `CsvImporter<T>.Import(TextReader, ...)` remains unchanged: callers that already provide decoded characters do not configure text encoding in the CSV parser.
- `CsvStreamImporterAdapter<T>` lives in **Infrastructure** and wraps the existing Application-owned `CsvImporter<T>`; it constructs a `StreamReader` and hands the decoded text to the ordinary importer.
- The adapter accepts any explicitly provided `System.Text.Encoding` available on the target .NET Framework runtime. The acceptance baseline covers UTF-8 and the legacy ISO-8859-1 encoding. No ambient machine code page, new encoding package or CSV-library-specific encoding model is used.
- The caller owns the input stream. Disposing the temporary `StreamReader` must not close it.
- The caller-provided `Encoding` is cloned so decoder fallback configuration cannot change the caller's instance. BOM detection cannot override the explicitly selected encoding.

## Error and policy contracts

- **Strict decoding is the default** (`strictDecoding: true`): invalid byte sequences produce a source-scoped `SOURCE_DECODING_FAILED` diagnostic owned by Domain. The fixed resource-backed message never includes bytes, raw record values or exception text.
- `strictDecoding: false` explicitly uses the framework's replacement fallback, which permits lossy decoding. This policy is not inferred from system settings.
- The source is buffered and parsed before typed materialization. A strict decode failure has status `Failed`, no accepted objects and zero processed, accepted and rejected record counts; it must not be reported as a record conversion failure.
- Numeric/date culture, delimiter configuration, header policies, schema binding and the existing decoded-character budget remain separate concerns.
- Decoder exception handling belongs in Infrastructure; Domain supplies the stable identifier, diagnostic scope, result model and safe message.

## Evidence

[Draft PR #136](https://github.com/infoconex/RecordSift/pull/136) demonstrated RED first: [Windows #41164696127](https://github.com/infoconex/RecordSift/actions/runs/41164696127) rejected the missing byte-source adapter (CS0246). After implementation, [Windows GREEN #41164887781](https://github.com/infoconex/RecordSift/actions/runs/41164887781) passed 213/213 NUnit tests (88 Domain, 39 Application, 86 Infrastructure). The real CSV acceptance tests verify ISO-8859-1 non-ASCII names, strict UTF-8 failure classification, no partial typed records and explicit replacement behavior.
