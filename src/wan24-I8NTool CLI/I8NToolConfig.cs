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
                // Attributes
                new KeywordParserPattern()
                {
                    Pattern = @"(Description|DisplayText)\(\s*\"".*[^\\]\""\s*\)",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*(Description|DisplayText)\(\s*(\"".*[^\\]\"")\s*\).*$",
                    Options = RegexOptions.None,
                    Replacement = "$2"
                },
                // Translation methods
                new KeywordParserPattern()
                {
                    Pattern = @"(__?|gettextn?|Translate(Plural)?|GetTerm|StdIn|StdOut)\(\s*\"".*[^\\]\""",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*(__?|gettextn?|Translate(Plural)?|GetTerm|StdIn|StdOut)\(\s*(\"".*[^\\]\"").*$",
                    Options = RegexOptions.None,
                    Replacement = "$3"
                },
                // CliApi attribute examples
                new KeywordParserPattern()
                {
                    Pattern = @"CliApi[^\(]*\([^\)]*Example\s*\=\s*\"".*[^\\]\""",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*CliApi[^\(]*\([^\)]*Example\s*\=\s*(\"".*[^\\]\"").*$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
                },
                // ExitCode attribute examples
                new KeywordParserPattern()
                {
                    Pattern = @"ExitCode[^\(]*\(\d+,\s*\"".*[^\\]\""",
                    Options = RegexOptions.None
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*ExitCode[^\(]*\(\d+,\s*(\"".*[^\\]\"").*$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
                },
                // Forced strings
                new KeywordParserPattern()
                {
                    Pattern = @"[^\@\$]\"".*[^\\]\"".*;.*\/\/.*wan24I8NTool\:include",
                    Options = RegexOptions.IgnoreCase
                },
                new KeywordParserPattern()
                {
                    Pattern = @"^.*[^\@\$](\"".*[^\\]\"").*;.*\/\/.*wan24I8NTool\:include.*$",
                    Options = RegexOptions.IgnoreCase,
                    Replacement = "$1"
                },
                // Cut the tail of multiple possible keywords within one line to get only one, finally
                new KeywordParserPattern()
                {
                    Pattern = @"^\s*(\"".*[^\\]\"").+$",
                    Options = RegexOptions.None,
                    Replacement = "$1"
                }
                ];
            FileExtensions = [".cs", ".razor", ".cshtml", ".aspx", ".cake", ".vb"];
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
        /// Merge the PO contents with an existing output PO file?
        /// </summary>
        public static bool MergeOutput { get; set; }

        /// <summary>
        /// Fail the whole process on any error?
        /// </summary>
        public static bool FailOnError { get; set; }
    }
}
