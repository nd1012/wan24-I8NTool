using wan24.Compression;
using wan24.Core;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8N
{
    /// <summary>
    /// i8n keyword catalog translation terms
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="catalog">Catalog</param>
    public class I8NCompressedTranslationTerms(in Dictionary<string, string[]> catalog) : PluralTranslationTerms(catalog)
    {
        /// <summary>
        /// i8n version number
        /// </summary>
        public const int VERSION = 1;
        /// <summary>
        /// i8n file structure header byte compression flag (bit 8)
        /// </summary>
        public const int HEADER_COMPRESSION_FLAG = 128;

        /// <summary>
        /// Create from stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="noHeader">Skip reading the header</param>
        /// <param name="uncompress">Uncompress?</param>
        /// <returns>Translation terms</returns>
        public static I8NCompressedTranslationTerms FromStream(in Stream stream, in bool noHeader = false, in bool uncompress = true)
        {
            // Header
            int version;
            bool compressed;
            if (!noHeader)
            {
                // Require header
                if (Trace) WriteTrace("Reading i8n header");
                int header = stream.ReadByte();
                compressed = (header & HEADER_COMPRESSION_FLAG) == HEADER_COMPRESSION_FLAG;
                version = header & ~HEADER_COMPRESSION_FLAG;
                if (Trace) WriteTrace($"Red header {header} (version {version}, compressed: {compressed})");
                if (version > VERSION) throw new InvalidDataException($"Can't read i8n file version #{version} (compressed: {compressed})");
                if (uncompress && !compressed) WriteWarning("Input i8n source wasn't compressed - can't uncompress");
            }
            else
            {
                // Skip header
                if (Trace) WriteTrace("Skip reading i8n header");
#pragma warning disable IDE0059 // Not required
                version = VERSION;
#pragma warning restore IDE0059 // Not required
                compressed = uncompress;
            }
            // Body
            CompressionOptions? options;
            Dictionary<string, string[]> res;
            if (compressed)
            {
                // Compressed
                if (Trace) WriteTrace("Decompress the i8n input source");
                using MemoryPoolStream ms = new();
                options = CompressionHelper.GetDefaultOptions();
                options.FlagsIncluded = true;
                options.AlgorithmIncluded = true;
                options.UncompressedLengthIncluded = true;
                options.LeaveOpen = true;
                options = CompressionHelper.ReadOptions(stream, ms, options);
                if (Trace)
                {
                    WriteTrace($"i8n compression algorithm: {options.Algorithm}");
                    WriteTrace($"i8n uncompressed length: {options.UncompressedDataLength}");
                }
                using Stream decompression = CompressionHelper.GetDecompressionStream(stream, options);
                if (Trace) WriteTrace("Deserialize the compressed i8n input source");
                res = JsonHelper.Decode<Dictionary<string, string[]>>(decompression)
                    ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from i8n input source");
            }
            else
            {
                // Uncompressed
                if (Trace) WriteTrace("Deserialize the uncompressed i8n input source");
                res = JsonHelper.Decode<Dictionary<string, string[]>>(stream)
                    ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from i8n input source");
            }
            return new(res);
        }

        /// <summary>
        /// Create from stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="noHeader">Skip reading the header</param>
        /// <param name="uncompress">Uncompress?</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Translation terms</returns>
        public static async Task<I8NCompressedTranslationTerms> FromStreamAsync(
            Stream stream, 
            bool noHeader = false, 
            bool uncompress = true, 
            CancellationToken cancellationToken = default
            )
        {
            // Header
            int version;
            bool compressed;
            if (!noHeader)
            {
                // Require header
                if (Trace) WriteTrace("Reading i8n header");
                int header = stream.ReadByte();
                compressed = (header & HEADER_COMPRESSION_FLAG) == HEADER_COMPRESSION_FLAG;
                version = header & ~HEADER_COMPRESSION_FLAG;
                if (Trace) WriteTrace($"Red header {header} (version {version}, compressed: {compressed})");
                if (version > VERSION) throw new InvalidDataException($"Can't read i8n file version #{version} (compressed: {compressed})");
                if (uncompress && !compressed) WriteWarning("Input i8n source wasn't compressed - can't uncompress");
            }
            else
            {
                // Skip header
                if (Trace) WriteTrace("Skip reading i8n header");
#pragma warning disable IDE0059 // Not required
                version = VERSION;
#pragma warning restore IDE0059 // Not required
                compressed = uncompress;
            }
            // Body
            CompressionOptions? options;
            Dictionary<string, string[]> res;
            if (compressed)
            {
                // Compressed
                if (Trace) WriteTrace("Decompress the i8n input source");
                using MemoryPoolStream ms = new();
                options = CompressionHelper.GetDefaultOptions();
                options.FlagsIncluded = true;
                options.AlgorithmIncluded = true;
                options.UncompressedLengthIncluded = true;
                options.LeaveOpen = true;
                options = await CompressionHelper.ReadOptionsAsync(stream, ms, options, cancellationToken).DynamicContext();
                if (Trace)
                {
                    WriteTrace($"i8n compression algorithm: {options.Algorithm}");
                    WriteTrace($"i8n uncompressed length: {options.UncompressedDataLength}");
                }
                using Stream decompression = CompressionHelper.GetDecompressionStream(stream, options);
                if (Trace) WriteTrace("Deserialize the compressed i8n input source");
                res = await JsonHelper.DecodeAsync<Dictionary<string, string[]>>(decompression, cancellationToken).DynamicContext()
                    ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from i8n input source");
            }
            else
            {
                // Uncompressed
                if (Trace) WriteTrace("Deserialize the uncompressed i8n input source");
                res = await JsonHelper.DecodeAsync<Dictionary<string, string[]>>(stream, cancellationToken).DynamicContext()
                    ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from i8n input source");
            }
            return new(res);
        }
    }
}
