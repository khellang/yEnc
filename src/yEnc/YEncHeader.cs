namespace yEnc
{
    public class YEncHeader
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <param name="line"></param>
        /// <param name="part"></param>
        /// <param name="total"></param>
        public YEncHeader(string name, long size, int line, int? part, int? total)
        {
            Name = name;
            Size = size;
            Line = line;
            Part = part;
            Total = total;
        }

        /// <summary>
        /// Name of the file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// Length of the encoded lines.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// In case of multiple parts this contains the part number.
        /// </summary>
        public int? Part { get; }

        /// <summary>
        /// (1.2) In case of multiple parts this contains the total number of parts.
        /// </summary>
        public int? Total { get; }

    }
}