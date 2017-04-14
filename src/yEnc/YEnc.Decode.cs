using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace yEnc
{
    public static class YEnc
    {
        [PublicAPI, NotNull]
        public static readonly System.Text.Encoding DefaultEncoding = System.Text.Encoding.GetEncoding("iso-8859-1");

        private static readonly char[] headerSeparators = { ' ' };

        /// <summary>
        /// Decodes the specified stream, using <see cref="DefaultEncoding"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="YEncException">The decoding failed.</exception>
        [PublicAPI, NotNull, Pure]
        public static Task<YEncDecodedStream> Decode(
            [NotNull] Stream stream,
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
        public static Task<YEncDecodedStream> Decode(
            [NotNull] Stream stream,
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
        public static Task<YEncDecodedStream> Decode(
            [NotNull] IEnumerable<Stream> streams,
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
        public static async Task<YEncDecodedStream> Decode(
            [NotNull] IEnumerable<Stream> streams,
            [NotNull] System.Text.Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(streams, nameof(streams));
            Check.NotNull(encoding, nameof(encoding));

            string fileName = null;
            Stream decodedStream = null;
            var foundSinglePartFile = false;
            var foundFilePart = false;

            foreach (Stream encodedPartStream in streams)
            {
                cancellationToken.ThrowIfCancellationRequested();
                YEncDecodedPart part = await DecodePart(encodedPartStream, cancellationToken);

                if (fileName == null)
                {
                    fileName = part.Header.Name;
                }

                if (part.IsFilePart)
                {
                    if (foundSinglePartFile)
                    {
                        throw new YEncException("Unexpected file part.");
                    }
                    foundFilePart = true;
                    if (decodedStream == null)
                    {
                        // TODO: investigate seek error
                        // decodedStream = new MemoryBlockStream();
                        decodedStream = new MemoryStream();
                    }
                    decodedStream.Seek(part.Offset, SeekOrigin.Begin);
                    await decodedStream.WriteAsync(part.Data, 0, part.Data.Length, cancellationToken);
                }
                else
                {
                    if (foundFilePart)
                    {
                        throw new YEncException("Unexpected single-part file.");
                    }
                    if (foundSinglePartFile)
                    {
                        throw new YEncException("Unexpected second single-part file.");
                    }
                    foundSinglePartFile = true;
                    decodedStream = new MemoryStream(part.Data);
                }
            }

            if (decodedStream == null)
            {
                return null;
            }

            // rewind stream
            decodedStream.Seek(0, SeekOrigin.Begin);
            return new YEncDecodedStream(decodedStream, fileName);
        }

        /// <summary>
        /// Decodes a single part from the specified stream, using <see cref="DefaultEncoding"/>.
        /// </summary>
        /// <param name="encodedPartStream">The stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [PublicAPI, NotNull, Pure]
        public static Task<YEncDecodedPart> DecodePart(
            [NotNull] Stream encodedPartStream,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return DecodePart(encodedPartStream, DefaultEncoding, cancellationToken);
        }

        /// <summary>
        /// Decodes a single part from the specified stream, using the specified encoding.
        /// </summary>
        /// <param name="encodedPartStream">The stream.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [PublicAPI, NotNull, Pure]
        public static async Task<YEncDecodedPart> DecodePart(
            [NotNull] Stream encodedPartStream,
            [NotNull] System.Text.Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(encodedPartStream, nameof(encodedPartStream));
            Check.NotNull(encoding, nameof(encoding));

            cancellationToken.ThrowIfCancellationRequested();
            using (var reader = new StreamReader(encodedPartStream, encoding))
            {
                string headerLine = await ReadHeaderLine(reader, cancellationToken);
                if (headerLine == null)
                {
                    throw new YEncException("No yenc header found in input stream");
                }
                YEncHeader header = ParseHeader(headerLine);
                YEncFooter footer = null;
                byte[] decodedBytes = null;
                bool isPart = header.Part.HasValue;
                if (!isPart)
                {
                    // file is not split into parts, expect no part headers
                    // create buffer for entire file
                    decodedBytes = new byte[header.Size];
                }
                var decodedBytesIndex = 0;
                YEncPartHeader partHeader = null;
                string currentLine;
                while ((currentLine = await reader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (currentLine.StartsWith(YEncConstants.PartHeader))
                    {
                        if (!isPart)
                        {
                            throw new YEncException("Unexpected part header");
                        }
                        partHeader = ParsePartHeader(currentLine);

                        // create buffer for part
                        decodedBytes = new byte[partHeader.Length];
                        continue;
                    }
                    if (currentLine.StartsWith(YEncConstants.Footer))
                    {
                        footer = ParseFooter(currentLine);
                        footer.Validate(header, decodedBytes);
                        break;
                    }
                    decodedBytesIndex += DecodeLine(currentLine, encoding, decodedBytes, decodedBytesIndex);
                }
                return new YEncDecodedPart(header, partHeader, footer, decodedBytes);
            }
        }

        [PublicAPI, NotNull, Pure]
        public static YEncDecodedPart DecodePart([NotNull] IList<string> encodedLines) => 
            DecodePart(encodedLines, DefaultEncoding);

        [PublicAPI, NotNull, Pure]
        public static YEncDecodedPart DecodePart(
            [NotNull] IList<string> encodedLines,
            [NotNull] System.Text.Encoding encoding)
        {
            Check.NotNull(encodedLines, nameof(encodedLines));
            Check.NotNull(encoding, nameof(encoding));

            // skip until header found
            var encodedIndex = 0;
            YEncHeader header = null;
            for (int len = encodedLines.Count; encodedIndex < len; encodedIndex++)
            {
                if (!encodedLines[encodedIndex].StartsWith(YEncConstants.Header))
                {
                    continue;
                }
                header = ParseHeader(encodedLines[encodedIndex++]);
                break;
            }
            if (header == null)
            {
                throw new YEncException("No yenc header found in input stream");
            }

            YEncFooter footer = null;
            byte[] decodedBytes = null;
            bool isPart = header.Part.HasValue;
            if (!isPart)
            {
                // file is not split into parts, expect no part headers
                // create buffer for entire file
                decodedBytes = new byte[header.Size];
            }

            var decodedBytesIndex = 0;
            YEncPartHeader partHeader = null;

            for (int len = encodedLines.Count; encodedIndex < len; encodedIndex++)
            {
                string currentLine = encodedLines[encodedIndex];
                if (currentLine.StartsWith(YEncConstants.PartHeader))
                {
                    if (!isPart)
                    {
                        throw new YEncException("Unexpected part header");
                    }
                    partHeader = ParsePartHeader(currentLine);

                    // create buffer for part
                    decodedBytes = new byte[partHeader.Length];
                    continue;
                }
                if (currentLine.StartsWith(YEncConstants.Footer))
                {
                    footer = ParseFooter(currentLine);
                    footer.Validate(header, decodedBytes);
                    break;
                }
                decodedBytesIndex += DecodeLine(currentLine, encoding, decodedBytes, decodedBytesIndex);
            }
            return new YEncDecodedPart(header, partHeader, footer, decodedBytes);
        }

        private static async Task<string> ReadHeaderLine(TextReader reader, CancellationToken cancellationToken)
        {
            string currentLine;
            while ((currentLine = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (currentLine.StartsWith(YEncConstants.Header))
                {
                    return currentLine;
                }
            }
            return null;
        }

        private static YEncHeader ParseHeader(string currentLine)
        {
            IDictionary<string, string> header = ParseLine(currentLine);
            string name = header.GetOrDefault(YEncConstants.Name);
            long size = header.GetAndConvert(YEncConstants.Size, long.Parse);
            int line = header.GetAndConvert(YEncConstants.Line, int.Parse);
            var part = header.GetAndConvert<int?>(YEncConstants.Part, p => int.Parse(p));
            var total = header.GetAndConvert<int?>(YEncConstants.Total, p => int.Parse(p));
            return new YEncHeader(name, size, line, part, total);
        }

        private static YEncPartHeader ParsePartHeader(string currentLine)
        {
            IDictionary<string, string> partHeader = ParseLine(currentLine);
            long begin = partHeader.GetAndConvert(YEncConstants.Begin, long.Parse);
            long end = partHeader.GetAndConvert(YEncConstants.End, long.Parse);
            return new YEncPartHeader(begin, end);
        }

        private static YEncFooter ParseFooter(string currentLine)
        {
            IDictionary<string, string> footer = ParseLine(currentLine);
            long size = footer.GetAndConvert(YEncConstants.Size, long.Parse);
            var part = footer.GetAndConvert<int?>(YEncConstants.Part, p => int.Parse(p));
            var crc32 = footer.GetAndConvert<uint?>(YEncConstants.Crc32, crc => Convert.ToUInt32(crc, 16));
            var partCrc32 = footer.GetAndConvert<uint?>(YEncConstants.PartCrc32, crc => Convert.ToUInt32(crc, 16));
            return new YEncFooter(size, part, crc32, partCrc32);
        }

        private static int DecodeLine(string currentLine, System.Text.Encoding encoding, byte[] decodedBytes, int decodedBytesIndex)
        {
            int saveIndex = decodedBytesIndex;
            byte[] encodedBytes = encoding.GetBytes(currentLine);
            var isEscaped = false;
            // ReSharper disable once ForCanBeConvertedToForeach
            // because better performance
            for (var i = 0; i < encodedBytes.Length; i++)
            {
                byte @byte = encodedBytes[i];
                if (@byte == 61 && !isEscaped)
                {
                    isEscaped = true;
                    continue;
                }
                if (isEscaped)
                {
                    isEscaped = false;
                    @byte = (byte)(@byte - 64);
                }
                decodedBytes[decodedBytesIndex++] = (byte)(@byte - 42);
            }
            return decodedBytesIndex - saveIndex;
        }

        private static IDictionary<string, string> ParseLine(string currentLine)
        {
            var dictionary = new Dictionary<string, string>();
            if (currentLine == null)
            {
                return dictionary;
            }

            // name is always last item on the header line
            string[] nameSplit = currentLine.Split(new[] { $"{YEncConstants.Name}=" }, StringSplitOptions.RemoveEmptyEntries);
            if (nameSplit.Length == 0)
            {
                return dictionary;
            }
            if (nameSplit.Length > 1)
            {
                // found name
                dictionary.Add(YEncConstants.Name, nameSplit[1].Trim());
            }

            // parse other items
            string[] pairs = nameSplit[0].Split(headerSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (pairs.Length == 0)
            {
                return dictionary;
            }
            for (var i = 1; i < pairs.Length; i++)
            {
                string[] parts = pairs[i].Split('=');
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