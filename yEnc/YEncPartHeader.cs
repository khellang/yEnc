namespace yEnc
{
    internal struct YEncPartHeader
    {
        private readonly long _begin;

        private readonly long _end;

        public YEncPartHeader(long begin, long end)
        {
            _begin = begin;
            _end = end;
        }

        public long Begin
        {
            get { return _begin; }
        }

        public long End
        {
            get { return _end; }
        }
    }
}