using wan24.Core;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8N
{
    /// <summary>
    /// i8n JSON translation terms
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="catalog">Catalog</param>
    public class I8NTranslationTerms(in Dictionary<string,string[]> catalog) : PluralTranslationTerms(catalog)
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
        /// <returns>Translation terms</returns>
        public static I8NTranslationTerms FromStream(in Stream stream, in bool noHeader = false)
        {
            // Header
            if (!noHeader)
            {
                // Require header
                if (Trace) WriteTrace("Reading i8n header");
                int header = stream.ReadByte();
                if (header < 1) throw new InvalidDataException("Failed to read the i8n header byte");
                if ((header & HEADER_COMPRESSION_FLAG) == HEADER_COMPRESSION_FLAG)
                    throw new NotSupportedException("The i8n stream is comressed - please use wan24-I8N-Compressed (I8NCompressedTranslationTerms) instead");
                if (Trace) WriteTrace($"Red i8n header {header} (version {header})");
                if (header > VERSION) throw new InvalidDataException($"Can't read file version #{header}");
            }
            else if (Trace)
            {
                WriteTrace("Skip reading i8n header");
            }
            // Body
            if (Trace) WriteTrace("Deserialize the uncompressed i8n input source");
            return new(JsonHelper.Decode<Dictionary<string, string[]>>(stream)
                ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from i8n input source"));
        }

        /// <summary>
        /// Create from stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="noHeader">Skip reading the header</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Translation terms</returns>
        public static async Task<I8NTranslationTerms> FromStreamAsync(Stream stream, bool noHeader = false, CancellationToken cancellationToken = default)
        {
            // Header
            if (!noHeader)
            {
                // Require header
                if (Trace) WriteTrace("Reading i8n header");
                int header = stream.ReadByte();
                if (header < 1) throw new InvalidDataException("Failed to read the i8n header byte");
                if ((header & HEADER_COMPRESSION_FLAG) == HEADER_COMPRESSION_FLAG)
                    throw new NotSupportedException("The i8n stream is comressed - please use wan24-I8N-Compressed (I8NCompressedTranslationTerms) instead");
                if (Trace) WriteTrace($"Red i8n header {header} (version {header})");
                if (header > VERSION) throw new InvalidDataException($"Can't read file version #{header}");
            }
            else if (Trace)
            {
                WriteTrace("Skip reading i8n header");
            }
            // Body
            if (Trace) WriteTrace("Deserialize the uncompressed i8n input source");
            return new(await JsonHelper.DecodeAsync<Dictionary<string, string[]>>(stream, cancellationToken)
                ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from i8n input source"));
        }
    }
}
