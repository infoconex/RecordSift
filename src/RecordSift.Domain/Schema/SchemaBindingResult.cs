using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RecordSift.Domain.Diagnostics;

namespace RecordSift.Domain.Schema
{
    /// <summary>Immutable outcome of binding declared columns to supplied header metadata.</summary>
    public sealed class SchemaBindingResult
    {
        internal SchemaBindingResult(List<SourceColumnBinding> bindings, List<ImportDiagnostic> diagnostics)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            IsSuccessful = diagnostics.Count == 0;
            // Never expose partially bound columns on a failed binding attempt.
            Bindings = new ReadOnlyCollection<SourceColumnBinding>(
                IsSuccessful ? new List<SourceColumnBinding>(bindings) : new List<SourceColumnBinding>());
            Diagnostics = new ReadOnlyCollection<ImportDiagnostic>(new List<ImportDiagnostic>(diagnostics));
        }

        /// <summary>Gets whether every required declaration was resolved without binding errors.</summary>
        public bool IsSuccessful { get; private set; }
        /// <summary>Gets bound source indexes in declaration order; empty on any binding failure.</summary>
        public IReadOnlyList<SourceColumnBinding> Bindings { get; private set; }
        /// <summary>Gets a read-only snapshot of source-scope binding errors.</summary>
        public IReadOnlyList<ImportDiagnostic> Diagnostics { get; private set; }
    }
}
