using RecordSift.Domain.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        public bool IsSuccessful { get; private set; }
        public IReadOnlyList<SourceColumnBinding> Bindings { get; private set; }
        public IReadOnlyList<ImportDiagnostic> Diagnostics { get; private set; }
    }
}
