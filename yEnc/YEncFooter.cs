using System.IO;

namespace yEnc
{
    internal struct YEncFooter
    {
        private readonly uint? _crc32;

        private readonly int? _part;

        private readonly uint? _partCrc32;

        private readonly long _size;

        public YEncFooter(long size, int? part, uint? crc32, uint? partCrc32)
        {
            _size = size;
            _part = part;
            _crc32 = crc32;
            _partCrc32 = partCrc32;
        }

        public long Size
        {
            get { return _size; }
        }

        public int? Part
        {
            get { return _part; }
        }

        public uint? Crc33
        {
            get { return _crc32; }
        }

        public uint? PartCrc32
        {
            get { return _partCrc32; }
        }

        public void Validate(YEncHeader header, MemoryStream decodedPartStream)
        {
            if (!MatchesPart(header))
            {
                throw new YEncException(string.Format("Part mismatch. Expected {0}, but got {1}.", header.Part, Part));
            }

            if (Size != decodedPartStream.Length)
            {
                throw new YEncException(string.Format("Size mismatch. Expected {0}, but got {1}.", Size, decodedPartStream.Length));
            }

            var crc32 = Crc33 ?? PartCrc32;
            if (crc32.HasValue)
            {
                var calculatedCrc32 = Crc32.CalculateChecksum(decodedPartStream.ToArray());
                if (calculatedCrc32 != crc32.Value)
                {
                    throw new YEncException(string.Format("Checksum mismatch. Expected {0}, but got {1}.", crc32.Value, calculatedCrc32));
                }
            }
        }

        private bool MatchesPart(YEncHeader header)
        {
            if (header.Part.HasValue)
            {
                return Part.HasValue && header.Part.Value == Part.Value;
            }

            return !Part.HasValue;
        }
    }
}