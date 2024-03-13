using System.Text.RegularExpressions;

namespace wan24.I8NTool
{
    /// <summary>
    /// i8n tool keyword parser pattern
    /// </summary>
    public sealed record class KeywordParserPattern
    {
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
        public Regex Expression => _Expression ??= new(Pattern, RegexOptions.Compiled | RegexOptions.Singleline | Options);

        /// <summary>
        /// Replacement pattern (only if this is a post-processing expression, which won't be used for the matching pre-process)
        /// </summary>
        public string? Replacement { get; init; }
    }
}
