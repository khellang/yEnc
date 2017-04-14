namespace yEnc
{
    public class YEncDecodedPart
    {
        public YEncHeader Header { get; }
        public YEncPartHeader PartHeader { get; }
        public YEncFooter Footer { get; }
        public byte[] Data { get; }

        public YEncDecodedPart(YEncHeader header, YEncPartHeader partHeader, YEncFooter footer, byte[] data)
        {
            Header = header;
            PartHeader = partHeader;
            Footer = footer;
            Data = data;
        }

        public bool IsFilePart => Header.Part.HasValue;
        public long Offset => IsFilePart ? PartHeader.Begin - 1 : 0L;
    }
}
