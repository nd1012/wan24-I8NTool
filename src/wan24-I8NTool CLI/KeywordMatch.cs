using wan24.Core;

namespace wan24.I8NTool
{
    /// <summary>
    /// Parser match
    /// </summary>
    public sealed record class KeywordMatch
    {
        /// <summary>
        /// Literal keyword
        /// </summary>
        private string? _KeywordLiteral = null;
        /// <summary>
        /// Quoted literal keyword
        /// </summary>
        private string? _KeywordQuotedLiteral = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public KeywordMatch() { }

        /// <summary>
        /// Keyword
        /// </summary>
        public required string Keyword { get; init; }

        /// <summary>
        /// Literal keyword
        /// </summary>
        public string KeywordLiteral => _KeywordLiteral ??= Keyword.ToLiteral();

        /// <summary>
        /// Quoted literal keyword
        /// </summary>
        public string KeywordQuotedLiteral => _KeywordQuotedLiteral ??= $"\"{KeywordLiteral}\"";

        /// <summary>
        /// Positions
        /// </summary>
        public HashSet<Position> Positions { get; init; } = [];

#if DEBUG
        /// <summary>
        /// Matching pattern indexes
        /// </summary>
        public HashSet<int> MatchingPatterns { get; init; } = [];

        /// <summary>
        /// Applied replacing pattern indexes
        /// </summary>
        public HashSet<int> ReplacingPatterns { get; init; } = [];
#endif

        /// <summary>
        /// Match position
        /// </summary>
        public sealed record class Position
        {
            /// <summary>
            /// Constructor
            /// </summary>
            public Position() { }

            /// <summary>
            /// Filename
            /// </summary>
            public string? FileName { get; init; }

            /// <summary>
            /// Line number
            /// </summary>
            public required int LineNumber { get; init; }
        }
    }
}
