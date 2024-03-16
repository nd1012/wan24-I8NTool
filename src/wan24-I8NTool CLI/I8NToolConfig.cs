using System.Text;

namespace wan24.I8NTool
{
    /// <summary>
    /// i8n tool configuration
    /// </summary>
    public static class I8NToolConfig
    {
        /// <summary>
        /// Constructor
        /// </summary>
        static I8NToolConfig()
        {
            Patterns = [
                // Cut out multiple possible keywords within one line to end up with only one
                new KeywordParserPattern()
                {
                    // Normalize a string literal (remove characters before and maybe after)
                    Pattern = KeywordParserPattern.NORMALIZE1,
                    Expression = KeywordParserPattern.RX_NORMALIZE1,
                    Replacement = KeywordParserPattern.NORMALIZE2,
                    ReplaceOnly = true
                },
                new KeywordParserPattern()
                {
                    // Normalize a string literal without characters before (remove characters after)
                    Pattern = KeywordParserPattern.NORMALIZE2,
                    Expression = KeywordParserPattern.RX_NORMALIZE2,
                    Replacement = KeywordParserPattern.NORMALIZE2_RPL,
                    ReplaceOnly = true
                },
                // Methods and attributes
                new KeywordParserPattern()
                {
                    Pattern = KeywordParserPattern.METHODS_AND_ATTRIBUTES,
                    Expression = KeywordParserPattern.RX_METHODS_AND_ATTRIBUTES,
                    Replacement = KeywordParserPattern.METHODS_AND_ATTRIBUTES_RPL
                },
                // Attribute properties
                new KeywordParserPattern()
                {
                    Pattern = KeywordParserPattern.ATTRIBUTE_PROPERTIES,
                    Expression = KeywordParserPattern.RX_ATTRIBUTE_PROPERTIES,
                    Replacement = KeywordParserPattern.ATTRIBUTE_PROPERTIES_RPL
                },
                // ExitCode attribute examples
                new KeywordParserPattern()
                {
                    Pattern = KeywordParserPattern.EXIT_CODE_ATTRIBUTE,
                    Expression = KeywordParserPattern.RX_EXIT_CODE_ATTRIBUTE,
                    Replacement = KeywordParserPattern.EXIT_CODE_ATTRIBUTE_RPL
                },
                // Include strings
                new KeywordParserPattern()
                {
                    Pattern = KeywordParserPattern.INCLUDE_STRINGS,
                    Expression = KeywordParserPattern.RX_INCLUDE_STRINGS,
                    Replacement = KeywordParserPattern.INCLUDE_STRINGS_RPL
                }
                ];
            FileExtensions = [".cs", ".razor", ".cshtml", ".aspx", ".cake", ".vb"];
            Exclude = ["*/obj/*"];
            SourceEncoding = Encoding.UTF8;
        }

        /// <summary>
        /// Use only a single thread?
        /// </summary>
        public static bool SingleThread { get; set; }

        /// <summary>
        /// Source text encoding
        /// </summary>
        public static Encoding SourceEncoding { get; set; }

        /// <summary>
        /// Parser patterns
        /// </summary>
        public static HashSet<KeywordParserPattern> Patterns { get; }

        /// <summary>
        /// File extensions to look for
        /// </summary>
        public static HashSet<string> FileExtensions { get; }

        /// <summary>
        /// Path to excluded source files (absolute path or filename only (\"*\" (any or none) and \"+\" (one or many) may be used as wildcard); case insensitive)
        /// </summary>
        public static HashSet<string> Exclude { get; }

        /// <summary>
        /// Merge the PO contents with an existing output PO file?
        /// </summary>
        public static bool MergeOutput { get; set; }

        /// <summary>
        /// Fail the whole process on any error?
        /// </summary>
        public static bool FailOnError { get; set; }
    }
}
