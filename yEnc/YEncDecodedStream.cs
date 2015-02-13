using System.Diagnostics;
using System.IO;

namespace yEnc
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class YEncDecodedStream : Stream
    {
        private readonly Stream _innerStream;

        internal YEncDecodedStream(Stream innerStream, string fileName)
        {
            FileName = fileName;
            _innerStream = innerStream;
        }

        public string FileName { get; private set; }

        public override bool CanRead
        {
            get { return _innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _innerStream.CanWrite; }
        }

        public override long Length
        {
            get { return _innerStream.Length; }
        }

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        private string DebuggerDisplay
        {
            get { return string.Format("FileName: {0}, Length: {1}", FileName, Length); }
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }
    }
}