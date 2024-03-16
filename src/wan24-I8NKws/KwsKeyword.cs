using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using wan24.Core;
using wan24.ObjectValidation;

namespace wan24.I8NKws
{
    /// <summary>
    /// KWS keyword record
    /// </summary>
    public sealed record class KwsKeyword
    {
        /// <summary>
        /// ID literal
        /// </summary>
        private string? _IdLiteral = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public KwsKeyword() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">ID</param>
        public KwsKeyword(in string id)
        {
            if (id.Length < 1) throw new ArgumentException("ID required", nameof(id));
            ID = id;
        }

        /// <summary>
        /// Get a plural translation
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>Translation</returns>
        public string this[in int count] => Plural(count);

        /// <summary>
        /// ID (keyword)
        /// </summary>
        [MinLength(1)]
        public string ID { get; private set; } = null!;

        /// <summary>
        /// ID literal
        /// </summary>
        [JsonIgnore, NoValidation]
        public string IdLiteral => _IdLiteral ??= ID.ToLiteral();

        /// <summary>
        /// Extracted time (UTC)
        /// </summary>
        public DateTime Extracted { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Updated time (UTC)
        /// </summary>
        public DateTime Updated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Latest translator name
        /// </summary>
        public string Translator { get; set; } = string.Empty;

        /// <summary>
        /// Developer comments
        /// </summary>
        public string DeveloperComments { get; set; } = string.Empty;

        /// <summary>
        /// Translator comments
        /// </summary>
        public string TranslatorComments { get; set; } = string.Empty;

        /// <summary>
        /// If this keyword is a large document
        /// </summary>
        public bool Document { get; set; }

        /// <summary>
        /// If this keyword is obsolete and should not be exported
        /// </summary>
        public bool Obsolete { get; set; }

        /// <summary>
        /// If the update of the ID has been done automatic using fuzzy logic search
        /// </summary>
        public bool Fuzzy { get; set; }

        /// <summary>
        /// If the keyword is invalid
        /// </summary>
        public bool Invalid { get; set; }

        /// <summary>
        /// If a translation is missing (no translation at all, or any empty translation)
        /// </summary>
        [JsonIgnore, NoValidation]
        public bool TranslationMissing => Translations.Count == 0 || Translations.Any(string.IsNullOrWhiteSpace);

        /// <summary>
        /// Translations
        /// </summary>
        public List<string> Translations { get; init; } = [];

        /// <summary>
        /// Source references
        /// </summary>
        public HashSet<KwsSourceReference> SourceReferences { get; init; } = [];

        /// <summary>
        /// Revisions of this keyword
        /// </summary>
        public HashSet<KwsKeyword> Revisions { get; init; } = [];

        /// <summary>
        /// Get a plural translation
        /// </summary>
        /// <param name="count">Count</param>
        /// <returns>Translation</returns>
        public string Plural(in int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (Translations.Count == 0) return string.Empty;
            if (count >= Translations.Count) return Translations[^1];
            return Translations[count];
        }

        /// <summary>
        /// Create a revision from this keyword
        /// </summary>
        public void CreateRevision()
        {
            if (Obsolete) throw new InvalidOperationException();
            Revisions.Add(this with
            {
                Revisions = []
            });
        }

        /// <summary>
        /// Restore a revision (will remove the revision and all newer revisions)
        /// </summary>
        /// <param name="revision">Revision to restore</param>
        public void RestoreRevision(in KwsKeyword revision)
        {
            if (!Revisions.Contains(revision)) throw new ArgumentException("Unknown revision", nameof(revision));
            Obsolete = false;
            ID = revision.ID;
            _IdLiteral = null;
            Extracted = revision.Extracted;
            Updated = revision.Updated;
            Translator = revision.Translator;
            DeveloperComments = revision.DeveloperComments;
            TranslatorComments = revision.TranslatorComments;
            Document = revision.Document;
            Fuzzy = revision.Fuzzy;
            Invalid = revision.Invalid;
            Translations.Clear();
            Translations.AddRange(revision.Translations);
            SourceReferences.Clear();
            SourceReferences.AddRange(revision.SourceReferences);
            KwsKeyword[] revisions = [.. Revisions];
            Revisions.Clear();
            for(int i = 0, len = revisions.Length; i < len; i++)
            {
                if (revisions[i] == revision) break;
                Revisions.Add(revisions[i]);
            }
        }

        /// <summary>
        /// Update the ID (wll create a new revision and clear source references, too; won't set <see cref="Updated"/>)
        /// </summary>
        /// <param name="newId">New ID</param>
        public void UpdateId(in string newId)
        {
            if (Obsolete) throw new InvalidOperationException();
            if (newId.Length < 1) throw new ArgumentException("ID required", nameof(newId));
            string oldId = ID;
            if (newId == oldId) return;
            CreateRevision();
            ID = newId;
            _IdLiteral = null;
            SourceReferences.Clear();
        }

        /// <summary>
        /// Validate the keyword
        /// </summary>
        /// <param name="throwOnError">Throw an exception on error?</param>
        /// <param name="requireCompleteTranslations">Require all translations to be complete?</param>
        /// <returns>If the keyword is valid</returns>
        /// <exception cref="InvalidDataException">Keyword is invalid</exception>
        public bool Validate(in bool throwOnError = true, in bool requireCompleteTranslations = false)
        {
            if (string.IsNullOrEmpty(ID))
            {
                if (!throwOnError) return false;
                throw new InvalidDataException("Missing keyword ID");
            }
            if (!Obsolete)
            {
                if (requireCompleteTranslations && TranslationMissing)
                {
                    if (!throwOnError) return false;
                    throw new InvalidDataException($"Missing translation of keyword \"{IdLiteral}\"");
                }
                if (Document && Translations.Count > 1)
                {
                    if (!throwOnError) return false;
                    throw new InvalidDataException($"Keyword \"{IdLiteral}\" is a document, plural isn't supported");
                }
            }
            if (!this.TryValidateObject(out List<ValidationResult> results, throwOnError: false))
            {
                if (!throwOnError) return false;
                throw new InvalidDataException($"Found {results.Count} keyword object \"{IdLiteral}\" errors - first error: {results.First().ErrorMessage}");
            }
            return true;
        }

        /// <summary>
        /// Merge with another keyword
        /// </summary>
        /// <param name="other">Other keyword</param>
        public void Merge(KwsKeyword other)
        {
            // Create a revision of the existing keyword
            CreateRevision();
            // Merge general meta data
            Updated = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(Translator) && !string.IsNullOrWhiteSpace(other.Translator))
                Translator = other.Translator;
            if (string.IsNullOrWhiteSpace(DeveloperComments) && !string.IsNullOrWhiteSpace(other.DeveloperComments))
                DeveloperComments = other.DeveloperComments;
            if (string.IsNullOrWhiteSpace(TranslatorComments) && !string.IsNullOrWhiteSpace(other.TranslatorComments))
                TranslatorComments = other.TranslatorComments;
            Document = other.Document;
            Obsolete = false;
            Fuzzy = other.Fuzzy;
            Invalid = other.Invalid;
            // Merge translations
            List<string> translations = new(Math.Max(Translations.Count, other.Translations.Count));
            for (int i = 0, len = translations.Count; i < len; i++)
                if (i > Translations.Count)
                {
                    Translations.Add(other.Translations[i]);
                }
                else if (i > other.Translations.Count)
                {
                }
                else if (string.IsNullOrWhiteSpace(Translations[i]))
                {
                    Translations[i] = other.Translations[i];
                }
                else if (string.IsNullOrWhiteSpace(other.Translations[i]))
                {
                }
                else
                {
                    Translations[i] = other.Translations[i];
                }
            Translations.Clear();
            Translations.AddRange(translations);
            // Merge references
            HashSet<KwsSourceReference> references = [.. SourceReferences, .. other.SourceReferences];
            SourceReferences.Clear();
            SourceReferences.AddRange(references);
            // Merge revisions
            HashSet<KwsKeyword> revisions = [.. Revisions.Concat(other.Revisions).OrderBy(r => r.Updated).Distinct()];
            Revisions.Clear();
            Revisions.AddRange(revisions);
        }
    }
}
