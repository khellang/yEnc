namespace yEnc
{
    internal struct YEncHeader
    {
        private readonly int _line;

        private readonly string _name;

        private readonly int? _part;

        private readonly long _size;

        public YEncHeader(string name, long size, int line, int? part)
        {
            _name = name;
            _size = size;
            _line = line;
            _part = part;
        }

        public string Name
        {
            get { return _name; }
        }

        public long Size
        {
            get { return _size; }
        }

        public int Line
        {
            get { return _line; }
        }

        public int? Part
        {
            get { return _part; }
        }
    }
}