using System.ComponentModel.DataAnnotations;
using wan24.Core;
using wan24.ObjectValidation;

namespace wan24.I8NKws
{
    /// <summary>
    /// KWS catalog
    /// </summary>
    public sealed class KwsCatalog
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public KwsCatalog() { }

        /// <summary>
        /// Project name
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Created time (UTC)
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Modified time (UTC)
        /// </summary>
        public DateTime Modified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Translator name
        /// </summary>
        public string Translator { get; set; } = string.Empty;

        /// <summary>
        /// Locale identifier
        /// </summary>
        [RegularExpression(RegularExpressions.LOCALE_WITH_DASH)]
        public string Locale { get; set; } = "en-US";

        /// <summary>
        /// If the text is written right to left
        /// </summary>
        public bool RightToLeft { get; set; }

        /// <summary>
        /// Keywords
        /// </summary>
        public HashSet<KwsKeyword> Keywords { get; } = [];

        /// <summary>
        /// Validate the catalog
        /// </summary>
        /// <param name="throwOnError">Throw an exception on error?</param>
        /// <param name="requireCompleteTranslations">Require all translations to be complete?</param>
        /// <returns>If the catalog is valid</returns>
        /// <exception cref="InvalidDataException">Catalog is invalid</exception>
        public bool Validate(in bool throwOnError = true, in bool requireCompleteTranslations = false)
        {
            if (Keywords.Count == 0)
            {
                if (!throwOnError) return false;
                throw new InvalidDataException("Missing keywords");
            }
            foreach (KwsKeyword keyword in Keywords)
            {
                if (string.IsNullOrEmpty(keyword.ID))
                {
                    if (!throwOnError) return false;
                    throw new InvalidDataException("Found keyword with missing ID");
                }
                if (requireCompleteTranslations && keyword.TranslationMissing)
                {
                    if (!throwOnError) return false;
                    throw new InvalidDataException($"Missing translation of ID \"{keyword.ID}\"");
                }
            }
            if(!this.TryValidateObject(out List<ValidationResult> results, throwOnError: false))
            {
                if (!throwOnError) return false;
                throw new InvalidDataException($"Found {results.Count} object errors - first error: {results.First().ErrorMessage}");
            }
            return true;
        }
    }
}
