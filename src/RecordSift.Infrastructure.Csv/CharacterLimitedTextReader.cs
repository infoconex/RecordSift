using System;
using System.IO;

namespace RecordSift.Infrastructure.Csv
{
    /// <summary>
    /// Counts decoded characters only; this wrapper owns neither the caller's reader
    /// nor CSV grammar. It permits an exact-budget EOF and detects a nonempty overrun.
    /// </summary>
    internal sealed class CharacterLimitedTextReader : TextReader
    {
        private readonly TextReader _source;
        private int _remaining;

        internal CharacterLimitedTextReader(TextReader source, int maxCharacters)
        {
            _source = source;
            _remaining = maxCharacters;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (count == 0)
                return 0;
            // Probe at most one character beyond the accepted budget.
            var requested = _remaining >= count ? count : _remaining + 1;
            var read = _source.Read(buffer, index, requested);
            if (read > _remaining)
                throw new CharacterLimitExceededException();
            _remaining -= read;
            return read;
        }

        public override int Read()
        {
            var value = _source.Read();
            if (value < 0)
                return value;
            if (_remaining == 0)
                throw new CharacterLimitExceededException();
            _remaining--;
            return value;
        }

        protected override void Dispose(bool disposing)
        {
            // Explicitly do not dispose caller-owned TextReader.
            base.Dispose(disposing);
        }
    }
}
