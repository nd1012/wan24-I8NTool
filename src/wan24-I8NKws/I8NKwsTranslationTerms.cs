using wan24.Core;

namespace wan24.I8NKws
{
    /// <summary>
    /// i8n keyword catalog translation terms
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="catalog">Catalog</param>
    public class I8NKwsTranslationTerms(in KwsCatalog catalog) : PluralTranslationTerms(catalog.ToDictionary())
    {
        /// <summary>
        /// Create from stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Catalog</returns>
        public static I8NKwsTranslationTerms FromStream(in Stream stream)
            => new(JsonHelper.Decode<KwsCatalog>(stream)
                ?? throw new InvalidDataException("Failed to decode catalog from stream"));

        /// <summary>
        /// Create from stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Catalog</returns>
        public static async Task<I8NKwsTranslationTerms> FromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
            => new(await JsonHelper.DecodeAsync<KwsCatalog>(stream, cancellationToken).DynamicContext()
                ?? throw new InvalidDataException("Failed to decode catalog from stream"));
    }
}
