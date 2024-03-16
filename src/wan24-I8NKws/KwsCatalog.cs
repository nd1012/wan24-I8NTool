using System.Collections;
using System.ComponentModel.DataAnnotations;
using wan24.Core;
using wan24.ObjectValidation;

namespace wan24.I8NKws
{
    /// <summary>
    /// KWS catalog
    /// </summary>
    public sealed record class KwsCatalog : IEnumerable<KwsKeyword>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public KwsCatalog() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dict">Dictionary</param>
        public KwsCatalog(in Dictionary<string, string[]> dict) : this()
        {
            Keywords.AddRange(from kvp in dict
                              select new KwsKeyword(kvp.Key)
                              {
                                  Translations = [.. kvp.Value]
                              });
        }

        /// <summary>
        /// Get a keyword
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>Keyword</returns>
        public KwsKeyword? this[string id] => Keywords.FirstOrDefault(k => k.ID == id);

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
        /// Locale identifier (<c>en-US</c> for example)
        /// </summary>
        [RegularExpression(RegularExpressions.LOCALE_WITH_DASH)]
        public string Locale { get; set; } = "en-US";

        /// <summary>
        /// If the text is written right to left
        /// </summary>
        public bool RightToLeft { get; set; }

        /// <summary>
        /// Complete keywords (non-obsolete)
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> CompleteKeywords => Keywords.Where(k => !k.Obsolete && !k.TranslationMissing);

        /// <summary>
        /// Incomplete keywords (non-obsolete)
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> IncompleteKeywords => Keywords.Where(k => !k.Obsolete && k.TranslationMissing);

        /// <summary>
        /// Revisioned keywords (non-onsolete)
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> RevisionedKeywords => Keywords.Where(k => !k.Obsolete && k.Revisions.Count > 0);

        /// <summary>
        /// Fuzzy keywords (non-onsolete)
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> FuzzyKeywords => Keywords.Where(k => !k.Obsolete && k.Fuzzy);

        /// <summary>
        /// Invalid keywords (non-onsolete)
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> InvalidKeywords => Keywords.Where(k => !k.Obsolete && k.Invalid);

        /// <summary>
        /// Keywords with developer comments (non-onsolete)
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> KeywordsWithDeveloperComments => Keywords.Where(k => !k.Obsolete && !string.IsNullOrWhiteSpace(k.DeveloperComments));

        /// <summary>
        /// Obsolete keywords
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> ObsoleteKeywords => Keywords.Where(k => k.Obsolete);

        /// <summary>
        /// Non-obsolete keywords
        /// </summary>
        [NoValidation]
        public IEnumerable<KwsKeyword> NonObsoleteKeywords => Keywords.Where(k => !k.Obsolete);

        /// <summary>
        /// Keywords
        /// </summary>
        [NoValidation]
        public HashSet<KwsKeyword> Keywords { get; init; } = [];

        /// <summary>
        /// Try adding a new keyword
        /// </summary>
        /// <param name="keyword">Keyword</param>
        /// <returns>If added</returns>
        public bool TryAddKeyword(in KwsKeyword keyword) => GetOrAddKeyword(keyword) == keyword;

        /// <summary>
        /// Add a new keyword
        /// </summary>
        /// <param name="keyword">Keyword</param>
        /// <returns>Added or existing keyword</returns>
        public KwsKeyword GetOrAddKeyword(KwsKeyword keyword)
        {
            if (Keywords.FirstOrDefault(k => k.ID == keyword.ID) is KwsKeyword existing) return existing;
            Keywords.Add(keyword);
            return keyword;
        }

        /// <summary>
        /// Remove a keyword
        /// </summary>
        /// <param name="id">iD</param>
        /// <returns>If removed</returns>
        public bool TryRemoveKeyword(string id)
        {
            KwsKeyword[] keywords = [.. Keywords];
            bool removed = false;
            for(int i = 0, len = keywords.Length; i < len; i++)
            {
                if (keywords[i].ID == id)
                {
                    removed = true;
                    continue;
                }
                Keywords.Add(keywords[i]);
            }
            return removed;
        }

        /// <summary>
        /// Determine if a keyword is contained
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>If the keyword is contained</returns>
        public bool ContainsKeyWord(string id) => Keywords.Any(k => k.ID == id);

        /// <summary>
        /// Validate the catalog (any keyword is required)
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
                if (!keyword.Validate(throwOnError, requireCompleteTranslations))
                    return false;
            if(!this.TryValidateObject(out List<ValidationResult> results, throwOnError: false))
            {
                if (!throwOnError) return false;
                throw new InvalidDataException($"Found {results.Count} catalog object errors - first error: {results.First().ErrorMessage}");
            }
            return true;
        }

        /// <summary>
        /// Merge with another catalog (existing keywords will be revisioned for merging)
        /// </summary>
        /// <param name="other">Other catalog to merge into this catalog</param>
        /// <param name="ignoreLocale">Ignore merging with another locale?</param>
        /// <returns>Merged existing keywords</returns>
        public KwsKeyword[] Merge(KwsCatalog other, bool ignoreLocale = false)
        {
            if (!ignoreLocale && other.Locale != Locale) throw new ArgumentException("Locale mismatch", nameof(other));
            List<KwsKeyword> res = [];
            Modified = DateTime.UtcNow;
            // Merge the catalog meta data
            if (string.IsNullOrWhiteSpace(Project) && !string.IsNullOrWhiteSpace(other.Project)) Project = other.Project;
            if (string.IsNullOrWhiteSpace(Translator) && !string.IsNullOrWhiteSpace(other.Translator)) Translator = other.Translator;
            RightToLeft = other.RightToLeft;
            // Merge keywords
            foreach(KwsKeyword otherKeyword in other.NonObsoleteKeywords)
            {
                if (this[otherKeyword.ID] is not KwsKeyword existing)
                {
                    Keywords.Add(otherKeyword);
                    continue;
                }
                existing.Merge(otherKeyword);
                res.Add(existing);
            }
            return [.. res];
        }

        /// <summary>
        /// Convert to a dictionary (only non-obsolete keywords)
        /// </summary>
        /// <returns>Dictionary</returns>
        public Dictionary<string, string[]> ToDictionary()
            => new(from keyword in NonObsoleteKeywords select new KeyValuePair<string, string[]>(keyword.ID, [.. keyword.Translations]));

        /// <inheritdoc/>
        public IEnumerator<KwsKeyword> GetEnumerator() => Keywords.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => Keywords.GetEnumerator();

        /// <summary>
        /// Cast as keyword count
        /// </summary>
        /// <param name="catalog">Catalog</param>
        public static implicit operator int(in KwsCatalog catalog) => catalog.Keywords.Count;
    }
}
