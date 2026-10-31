# Third-party dependencies and license notices — RecordSift 0.1

RecordSift-owned source and distributed assemblies are licensed under the [MIT License](LICENSE), copyright (c) 2026 Infoconex.

The RecordSift NuGet package declares the following **separately installed** NuGet dependencies; their binaries are **not bundled into RecordSift's own `lib/net48` package folder**. Each dependency is governed by its own license. This file identifies the published licenses and sources, and does not replace the actual notices or license files in the third-party packages.

| Package | Pinned version | Published license | Version-specific NuGet source |
| --- | --- | --- | --- |
| CsvHelper | 33.1.0 | MS-PL OR Apache-2.0 | https://www.nuget.org/packages/CsvHelper/33.1.0 |
| ExcelDataReader | 3.9.0 | MIT | https://www.nuget.org/packages/ExcelDataReader/3.9.0 |
| Microsoft.Bcl.AsyncInterfaces | 8.0.0 | MIT | https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/8.0.0 |
| Microsoft.Bcl.HashCode | 1.1.1 | MIT | https://www.nuget.org/packages/Microsoft.Bcl.HashCode/1.1.1 |
| Microsoft.CSharp | 4.7.0 | MIT | https://www.nuget.org/packages/Microsoft.CSharp/4.7.0 |
| System.Memory | 4.5.5 | MIT (CoreFX license link) | https://www.nuget.org/packages/System.Memory/4.5.5 |
| System.Buffers | 4.5.1 | MIT (CoreFX license link) | https://www.nuget.org/packages/System.Buffers/4.5.1 |
| System.Numerics.Vectors | 4.5.0 | MIT (CoreFX license link) | https://www.nuget.org/packages/System.Numerics.Vectors/4.5.0 |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | MIT | https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0 |
| System.Threading.Tasks.Extensions | 4.5.4 | MIT (CoreFX license link) | https://www.nuget.org/packages/System.Threading.Tasks.Extensions/4.5.4 |
| System.ValueTuple | 4.5.0 | MIT (CoreFX license link) | https://www.nuget.org/packages/System.ValueTuple/4.5.0 |

The CsvHelper package's `MS-PL OR Apache-2.0` expression describes an either/or licensing choice, **not** a requirement to comply with both simultaneously. For older Microsoft packages, their NuGet license-info links point to the MIT-licensed [.NET CoreFX license text](https://github.com/dotnet/corefx/blob/master/LICENSE.TXT). Review the license material shipped in each installed package for downstream redistribution.

This inventory is version-specific to the 0.1 release candidate. Changes to dependencies require a fresh inventory and security/license review. See [detailed dependency evidence](https://github.com/infoconex/RecordSift/blob/release/0.1/docs/release/0.1-dependencies.md) in the source repository.
