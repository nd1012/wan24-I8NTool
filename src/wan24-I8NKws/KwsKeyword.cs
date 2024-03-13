using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using wan24.Core;

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
        /// ID (keyword)
        /// </summary>
        [MinLength(1)]
        public string ID { get; private set; } = null!;

        /// <summary>
        /// ID literal
        /// </summary>
        public string IdLiteral => _IdLiteral ??= ID.ToLiteral();

        /// <summary>
        /// Previous IDs (extended when the ID is being updated; last entry was the latest ID)
        /// </summary>
        public HashSet<string> PreviousIds { get; private set; } = [];

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
        /// If this keyword is obsolete and should not be exported
        /// </summary>
        public bool Obsolete { get; set; }

        /// <summary>
        /// If the update of the ID has been done automatic using fuzzy logic search
        /// </summary>
        public bool Fuzzy { get; set; }

        /// <summary>
        /// If a translation is missing (no translation at all, or any empty translations)
        /// </summary>
        [JsonIgnore]
        public bool TranslationMissing => Translations.Count == 0 || Translations.Any(t => t.Length == 0);

        /// <summary>
        /// Translations
        /// </summary>
        public List<string> Translations { get; init; } = [];

        /// <summary>
        /// Source references
        /// </summary>
        public HashSet<KwsSourceReference> SourceReferences { get; init; } = [];

        /// <summary>
        /// Previous source references (if fuzzy logicwas used to update the ID)
        /// </summary>
        public HashSet<KwsSourceReference> PreviousSourceReferences { get; init; } = [];

        /// <summary>
        /// Update the ID (source references will be moved to <see cref="PreviousSourceReferences"/>)
        /// </summary>
        /// <param name="newId">New ID</param>
        public void UpdateId(in string newId)
        {
            if (Obsolete) throw new InvalidOperationException();
            if (newId.Length < 1) throw new ArgumentException("ID required", nameof(newId));
            string oldId = ID;
            if (newId == oldId) return;
            ID = newId;
            PreviousIds.Remove(oldId);
            PreviousIds.Add(oldId);
            PreviousSourceReferences.Clear();
            PreviousSourceReferences.AddRange(SourceReferences);
            SourceReferences.Clear();
        }

        /// <summary>
        /// Undo an ID update
        /// </summary>
        /// <param name="id">Target ID to use</param>
        public void UndoIdUpdate(string? id = null)
        {
            if (Obsolete || PreviousIds.Count == 0) throw new InvalidOperationException();
            bool lastId = id is null;
            if (lastId)
            {
                id = PreviousIds.Last();
            }
            else if (!PreviousIds.Contains(id!))
            {
                throw new ArgumentException("Unknown previous ID", nameof(id));
            }
            ID = id!;
            PreviousIds = [.. PreviousIds.SkipWhile(pid => pid != id).Skip(1)];
            SourceReferences.Clear();
            SourceReferences.AddRange(PreviousSourceReferences);
            PreviousSourceReferences.Clear();
        }
    }
}
