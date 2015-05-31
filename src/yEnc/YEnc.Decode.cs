using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace yEnc
{
    public static partial class YEnc
    {
        [PublicAPI, NotNull]
        public static readonly System.Text.Encoding DefaultEncoding = System.Text.Encoding.GetEncoding("iso-8859-1");

        private static readonly char[] HeaderSeparators = { ' ' };

        /// <summary>
        /// Decodes the specified stream, using <see cref="DefaultEncoding"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="YEncException">The decoding failed.</exception>
        [PublicAPI, NotNull, Pure]
        public static Task<YEncDecodedStream> Decode([NotNull] Stream stream,
            CancellationToken cancellationToken = default(CancellationToken)) =>
                Decode(stream, DefaultEncoding, cancellationToken);

        /// <summary>
        /// Decodes the specified stream, using the specified encoding.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="YEncException">The decoding failed.</exception>
        [PublicAPI, NotNull, Pure]
        public static Task<YEncDecodedStream> Decode([NotNull] Stream stream,
            [NotNull] System.Text.Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken)) =>
                Decode(new[] { Check.NotNull(stream, nameof(stream)) }, encoding, cancellationToken);

        /// <summary>
        /// Decodes the specified streams, using <see cref="DefaultEncoding" />.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="YEncException">The decoding failed.</exception>
        [PublicAPI, NotNull, Pure]
        public static Task<YEncDecodedStream> Decode([NotNull] IEnumerable<Stream> streams,
            CancellationToken cancellationToken = default(CancellationToken)) =>
                Decode(streams, DefaultEncoding, cancellationToken);

        /// <summary>
        /// Decodes the specified streams, using the specified encoding.
        /// </summary>
        /// <param name="streams">The streams.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="YEncException">The decoding failed.</exception>
        [PublicAPI, NotNull, Pure]
        public static async Task<YEncDecodedStream> Decode([NotNull] IEnumerable<Stream> streams,
            [NotNull] System.Text.Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(streams, nameof(streams));
            Check.NotNull(encoding, nameof(encoding));

            var decodedStream = new MemoryStream();

            var fileName = string.Empty;

            foreach (var encodedPartStream in streams)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var reader = new StreamReader(encodedPartStream, encoding))
                {
                    var header = await ParseHeader(reader, cancellationToken);
                    if (!header.HasValue)
                    {
                        continue;
                    }

                    fileName = header.Value.Name;

                    using (var decodedPartStream = new MemoryBlockStream())
                    {
                        string currentLine;
                        while ((currentLine = await reader.ReadLineAsync()) != null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (currentLine.StartsWith(YEncConstants.PartHeader))
                            {
                                var partHeader = ParsePartHeader(currentLine);

                                decodedStream.Seek(partHeader.Begin - 1L, SeekOrigin.Begin);
                                continue;
                            }

                            if (currentLine.StartsWith(YEncConstants.Footer))
                            {
                                ParseFooter(currentLine).Validate(header.Value, decodedPartStream);
                                break;
                            }

                            var decodedBytes = DecodeLine(currentLine, header.Value, encoding);

                            await decodedPartStream.WriteAsync(decodedBytes, 0, decodedBytes.Length, cancellationToken);
                        }

                        decodedPartStream.Seek(0, SeekOrigin.Begin);

                        await decodedPartStream.CopyToAsync(decodedStream, 4096, cancellationToken);
                    }
                }
            }

            decodedStream.Seek(0, SeekOrigin.Begin);

            return new YEncDecodedStream(decodedStream, fileName);
        }

        private static async Task<YEncHeader?> ParseHeader(TextReader reader, CancellationToken cancellationToken)
        {
            string currentLine;
            while ((currentLine = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!currentLine.StartsWith(YEncConstants.Header))
                {
                    continue;
                }

                var header = ParseLine(currentLine);

                var name = header.GetOrDefault(YEncConstants.Name);

                var size = header.GetAndConvert(YEncConstants.Size, s => long.Parse(s));

                var line = header.GetAndConvert(YEncConstants.Line, l => int.Parse(l));

                var part = header.GetAndConvert<int?>(YEncConstants.Part, p => int.Parse(p));

                return new YEncHeader(name, size, line, part);
            }

            return null;
        }

        private static YEncPartHeader ParsePartHeader(string line)
        {
            var partHeader = ParseLine(line);

            var begin = partHeader.GetAndConvert(YEncConstants.Begin, b => long.Parse(b));

            var end = partHeader.GetAndConvert(YEncConstants.End, e => long.Parse(e));

            return new YEncPartHeader(begin, end);
        }

        private static YEncFooter ParseFooter(string currentLine)
        {
            var footer = ParseLine(currentLine);

            var size = footer.GetAndConvert(YEncConstants.Size, s => long.Parse(s));

            var part = footer.GetAndConvert<int?>(YEncConstants.Part, p => int.Parse(p));

            var crc32 = footer.GetAndConvert<uint?>(YEncConstants.Crc32, crc => Convert.ToUInt32(crc, 16));

            var partCrc32 = footer.GetAndConvert<uint?>(YEncConstants.PartCrc32, crc => Convert.ToUInt32(crc, 16));

            return new YEncFooter(size, part, crc32, partCrc32);
        }

        private static byte[] DecodeLine(string currentLine, YEncHeader header, System.Text.Encoding encoding)
        {
            var encodedBytes = encoding.GetBytes(currentLine);

            var bytes = new List<byte>(header.Line);

            var isEscaped = false;

            for (var i = 0; i < encodedBytes.Length; i++)
            {
                var @byte = encodedBytes[i];

                if (@byte == 61 && !isEscaped)
                {
                    isEscaped = true;
                    continue;
                }

                if (isEscaped)
                {
                    isEscaped = false;
                    @byte = (byte) (@byte - 64);
                }

                bytes.Add((byte) (@byte - 42));
            }

            return bytes.ToArray();
        }

        private static IDictionary<string, string> ParseLine(string currentLine)
        {
            var dictionary = new Dictionary<string, string>();

            var pairs = currentLine.Split(HeaderSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (pairs.Length == 0)
            {
                return dictionary;
            }

            for (var i = 1; i < pairs.Length; i++)
            {
                var parts = pairs[i].Split('=');
                if (parts.Length < 2)
                {
                    continue;
                }

                dictionary.Add(parts[0], parts[1]);
            }

            return dictionary;
        }
    }
}