using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;

namespace yEnc
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class YEncDecodedStream : Stream
    {
        internal YEncDecodedStream([NotNull] Stream innerStream, [NotNull] string fileName)
        {
            InnerStream = Check.NotNull(innerStream, nameof(innerStream));
            FileName = Check.NotEmpty(fileName, nameof(fileName));
        }

        [NotNull]
        public string FileName { get; }

        private Stream InnerStream { get; }

        public override bool CanRead => InnerStream.CanRead;

        public override bool CanSeek => InnerStream.CanSeek;

        public override bool CanWrite => InnerStream.CanWrite;

        public override long Length => InnerStream.Length;

        public override long Position
        {
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        private string DebuggerDisplay => $"FileName: {FileName}, Length: {Length}";

        public override void Flush() => InnerStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);

        public override void SetLength(long value) => InnerStream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count) => InnerStream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count) => InnerStream.Write(buffer, offset, count);
    }
}