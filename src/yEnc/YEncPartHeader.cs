namespace yEnc
{
    internal struct YEncPartHeader
    {
        public YEncPartHeader(long begin, long end)
        {
            Begin = begin;
            End = end;
        }

        public long Begin { get; }

        public long End { get; }
    }
}