namespace yEnc
{
    public class YEncPartHeader
    {
        public YEncPartHeader(long begin, long end)
        {
            Begin = begin;
            End = end;
        }

        public long Begin { get; }

        public long End { get; }

        public long Length => End - Begin + 1;
    }
}