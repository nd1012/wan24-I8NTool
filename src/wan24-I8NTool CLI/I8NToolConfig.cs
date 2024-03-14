using System.Text;
using System.Text.RegularExpressions;

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
                // Methods and attributes
                new KeywordParserPattern()
                {
                    Pattern = @"(__?|gettextn?|Translate(Plural)?|GetTerm|Std(In|Out)|Description|DisplayText)\s*\(\s*\"".*[^\\]\""",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*(__?|gettextn?|Translate(Plural)?|GetTerm|Std(In|Out)|Description|DisplayText)\s*\(\s*(\"".*[^\\]\"").*$",
                    Options = RegexOptions.None,
                    Replacement = "$4"
                },
                // Attribute properties
                new KeywordParserPattern()
                {
                    Pattern = @"[^\(]*\([^\)]*(Example|ErrorMessage)\s*\=\s*\"".*[^\\]\""",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*[^\(]*\([^\)]*(Example|ErrorMessage)\s*\=\s*(\"".*[^\\]\"").*$",
                    Options = RegexOptions.None,
                    Replacement = "$2"
                },
                // ExitCode attribute examples
                new KeywordParserPattern()
                {
                    Pattern = @"ExitCode[^\(]*\(\d+\s*,\s*\"".*[^\\]\""",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*ExitCode[^\(]*\(\d+\s*,\s*(\"".*[^\\]\"").*$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
                },
                // Forced strings
                new KeywordParserPattern()
                {
                    Pattern = @"[^\@\$\""\\]*\s*\"".*[^\\]\"".*\/\/.*wan24I8NTool\:include",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*[^\@\$\""\\]*\s*(\"".*[^\\]\"").*\/\/.*wan24I8NTool\:include.*$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
                },
                // Cut out multiple possible keywords within one line to get only one, finally
                new KeywordParserPattern()
                {
                    Pattern = @"^(\"".*[^\\]\"").+$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*[^\@\$\""\\](\"".*[^\\]\"")$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
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
