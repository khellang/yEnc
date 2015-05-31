namespace yEnc
{
    internal struct YEncHeader
    {
        public YEncHeader(string name, long size, int line, int? part)
        {
            Name = name;
            Size = size;
            Line = line;
            Part = part;
        }

        public string Name { get; }

        public long Size { get; }

        public int Line { get; }

        public int? Part { get; }
    }
}