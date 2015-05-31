using System.IO;

namespace yEnc
{
    internal struct YEncFooter
    {
        public YEncFooter(long size, int? part, uint? crc32, uint? partCrc32)
        {
            Size = size;
            Part = part;
            Crc33 = crc32;
            PartCrc32 = partCrc32;
        }

        public long Size { get; }

        public int? Part { get; }

        public uint? Crc33 { get; }

        public uint? PartCrc32 { get; }

        public void Validate(YEncHeader header, MemoryStream decodedPartStream)
        {
            if (!MatchesPart(header))
            {
                throw new YEncException($"Part mismatch. Expected {header.Part}, but got {Part}.");
            }

            if (Size != decodedPartStream.Length)
            {
                throw new YEncException($"Size mismatch. Expected {Size}, but got {decodedPartStream.Length}.");
            }

            var crc32 = Crc33 ?? PartCrc32;
            if (crc32.HasValue)
            {
                var calculatedCrc32 = Crc32.CalculateChecksum(decodedPartStream.ToArray());
                if (calculatedCrc32 != crc32.Value)
                {
                    throw new YEncException($"Checksum mismatch. Expected {crc32.Value}, but got {calculatedCrc32}.");
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