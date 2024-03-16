using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using wan24.Core;
using wan24.ObjectValidation;

namespace wan24.I8NTool
{
    /// <summary>
    /// i8n tool keyword parser pattern
    /// </summary>
    public sealed partial record class KeywordParserPattern
    {
        /// <summary>
        /// Thread synchronization
        /// </summary>
        private readonly object Sync = new();
        /// <summary>
        /// Regular expression
        /// </summary>
        private Regex? _Expression = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public KeywordParserPattern() { }

        /// <summary>
        /// Regular expression pattern
        /// </summary>
        public required string Pattern { get; init; }

        /// <summary>
        /// Regular expression options
        /// </summary>
        public RegexOptions Options { get; init; } = RegexOptions.None;

        /// <summary>
        /// Regular expression
        /// </summary>
        [JsonIgnore, NoValidation]
        public Regex Expression
        {
            get
            {
                //TODO Parse only, if variables are being used (use wan24.Core.RegularExpressions.RX_PARSER_VAR)
                if (_Expression is null)
                    lock (Sync)
                        _Expression ??= new(Pattern.Parse(ParserData), RX_OPTIONS | Options, TimeSpan.FromSeconds(RX_TIMEOUT_S));
                return _Expression;
            }
            internal set => _Expression = value;
        }

        /// <summary>
        /// Replacement pattern (only if this is a post-processing expression, which won't be used for the matching pre-process)
        /// </summary>
        public string? Replacement { get; init; }

        /// <summary>
        /// If this is a replacement-only pattern
        /// </summary>
        public bool ReplaceOnly { get; init; }
    }
}
