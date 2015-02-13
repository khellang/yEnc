using System.Linq;
using JetBrains.Annotations;

namespace yEnc
{
    internal static class Crc32
    {
        private const uint Polynomial = 0xEDB88320;

        private const uint Seed = 0xFFFFFFFF;

        private static readonly uint[] LookupTable;

        static Crc32()
        {
            LookupTable = CreateLookupTable();
        }

        public static uint CalculateChecksum([NotNull] byte[] buffer)
        {
            Check.NotNull(buffer, "buffer");

            return buffer.Aggregate(Seed, (u, b) => Lookup(u, b), value => value ^ Seed);
        }

        private static uint Lookup(uint value, byte b)
        {
            return (value >> 8) ^ LookupTable[(value & 0xFF) ^ b];
        }

        private static uint[] CreateLookupTable()
        {
            var table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                var entry = i;

                for (var j = 0; j < 8; j++)
                {
                    entry = (entry & 1) == 1 ? (entry >> 1) ^ Polynomial : entry >> 1;
                }

                table[i] = entry;
            }

            return table;
        }
    }
}