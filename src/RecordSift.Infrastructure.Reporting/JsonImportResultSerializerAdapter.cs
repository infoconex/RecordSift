using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using RecordSift.Application.Ports.Reporting;
using RecordSift.Domain.Diagnostics;
using RecordSift.Domain.Results;

namespace RecordSift.Infrastructure.Reporting
{
    /// <summary>
    /// Version 1.0 JSON projection of the established Domain result contract.
    /// A replaceable adapter; no source file is reopened and no accepted CLR
    /// record values are serialized. All diagnostic messages come from Domain.
    /// </summary>
    public sealed class JsonImportResultSerializerAdapter : IImportResultSerializer
    {
        private const string JsonSchemaVersion = "1.0";
        private const string StoppedJsonSchemaVersion = "1.1";

        /// <summary>Projects the existing import result to stable schema-version 1.0 JSON. Accepted CLR records and caller-defined messages are omitted; rooted paths and file-URI source IDs are redacted, but arbitrary caller metadata is not scrubbed.</summary>
        /// <param name="result">Existing in-memory typed result; null throws an argument exception.</param>
        /// <typeparam name="T">Accepted CLR record type; values are not included in output.</typeparam>
        public string Serialize<T>(TypedImportResult<T> result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var diagnostics = new List<DiagnosticContract>();
            foreach (var item in result.Diagnostics)
            {
                diagnostics.Add(new DiagnosticContract
                {
                    Scope = item.Scope.ToString(),
                    Row = item.Row,
                    Field = item.Field,
                    Code = item.Code,
                    Message = item.Message,
                    Worksheet = item.Worksheet,
                    PhysicalRow = item.PhysicalRow,
                    PhysicalLine = item.PhysicalLine
                });
            }

            var report = new ReportContract
            {
                SchemaVersion = result.Status == ImportStatus.Stopped
                    ? StoppedJsonSchemaVersion : JsonSchemaVersion,
                SourceId = SafeSourceIdentifier(result.SourceId),
                Status = result.Status.ToString(),
                Message = SafeStatusMessage(result),
                Summary = new SummaryContract
                {
                    ProcessedCount = result.Summary.ProcessedCount,
                    AcceptedCount = result.Summary.AcceptedCount,
                    RejectedCount = result.Summary.RejectedCount
                },
                Diagnostics = diagnostics
            };

            var serializer = new DataContractJsonSerializer(typeof(ReportContract));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, report);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        // Avoid disclosing absolute local source paths, while preserving safe opaque IDs.
        private static string SafeSourceIdentifier(string sourceId)
        {
            bool rooted = sourceId.StartsWith("/", StringComparison.Ordinal) ||
                sourceId[0] == (char)92 ||
                sourceId.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                (sourceId.Length >= 3 && char.IsLetter(sourceId[0]) &&
                 sourceId[1] == ':' && (sourceId[2] == '/' || sourceId[2] == (char)92));
            return rooted ? RedactedSourceIdentifier : sourceId;
        }

        // Caller-supplied messages can contain arbitrary record values; never export them by default.
        private static string SafeStatusMessage<T>(TypedImportResult<T> result)
        {
            if (result.Status == ImportStatus.Stopped)
                return ReportingMessageResources.ImportStopped;
            if (result.Status == ImportStatus.Failed)
                return ReportingMessageResources.ImportFailed;
            if (result.Summary.ProcessedCount == 0)
                return ReportingMessageResources.ImportEmpty;
            return result.Summary.RejectedCount > 0
                ? ReportingMessageResources.ImportCompletedWithRejections
                : ReportingMessageResources.ImportCompleted;
        }

        private const string RedactedSourceIdentifier = "source-redacted";

        [DataContract]
        private sealed class ReportContract
        {
            [DataMember(Name = "schemaVersion", Order = 1)]
            public string SchemaVersion { get; set; }

            [DataMember(Name = "sourceId", Order = 2)]
            public string SourceId { get; set; }

            [DataMember(Name = "status", Order = 3)]
            public string Status { get; set; }

            [DataMember(Name = "message", Order = 4)]
            public string Message { get; set; }

            [DataMember(Name = "summary", Order = 5)]
            public SummaryContract Summary { get; set; }

            [DataMember(Name = "diagnostics", Order = 6)]
            public List<DiagnosticContract> Diagnostics { get; set; }
        }

        [DataContract]
        private sealed class SummaryContract
        {
            [DataMember(Name = "processedCount", Order = 1)]
            public int ProcessedCount { get; set; }

            [DataMember(Name = "acceptedCount", Order = 2)]
            public int AcceptedCount { get; set; }

            [DataMember(Name = "rejectedCount", Order = 3)]
            public int RejectedCount { get; set; }
        }

        [DataContract]
        private sealed class DiagnosticContract
        {
            [DataMember(Name = "scope", Order = 1)]
            public string Scope { get; set; }

            [DataMember(Name = "row", Order = 2)]
            public int? Row { get; set; }

            [DataMember(Name = "field", Order = 3)]
            public string Field { get; set; }

            [DataMember(Name = "code", Order = 4)]
            public string Code { get; set; }

            [DataMember(Name = "message", Order = 5)]
            public string Message { get; set; }

            [DataMember(Name = "worksheet", Order = 6, EmitDefaultValue = false)]
            public string Worksheet { get; set; }

            [DataMember(Name = "physicalRow", Order = 7, EmitDefaultValue = false)]
            public int? PhysicalRow { get; set; }

            [DataMember(Name = "physicalLine", Order = 8, EmitDefaultValue = false)]
            public int? PhysicalLine { get; set; }
        }
    }
}
