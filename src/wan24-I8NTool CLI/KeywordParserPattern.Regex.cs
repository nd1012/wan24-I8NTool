using System.Text.RegularExpressions;

namespace wan24.I8NTool
{
    // Regular expressions
    public sealed partial record class KeywordParserPattern
    {
        /// <summary>
        /// Regular expression timeout in ms
        /// </summary>
        public const int RX_TIMEOUT_MS = 3000;
        /// <summary>
        /// Regular expression timeout in seconds
        /// </summary>
        public const int RX_TIMEOUT_S = 3;
        /// <summary>
        /// Default regular expression options
        /// </summary>
        public const RegexOptions RX_OPTIONS = RegexOptions.Singleline | RegexOptions.Compiled;
        /// <summary>
        /// Normalize a string literal (remove characters before and maybe after)
        /// </summary>
        public const string NORMALIZE1 = @$"^.*[^\@\$\""\\]({STRING_LITERAL}).*$";
        /// <summary>
        /// Normalize a string literal without characters before (remove characters after)
        /// </summary>
        public const string NORMALIZE2 = @$"^({STRING_LITERAL}).+$";
        /// <summary>
        /// Methods and attributes
        /// </summary>
        public const string METHODS_AND_ATTRIBUTES = @$"^.*(_+|gettext[nd]?|Translate(Plural)?|GetTerm|Std(In|OutErr)|Description|DisplayText)\s*\(\s*({STRING_LITERAL}).*$";
        /// <summary>
        /// Attribute properties
        /// </summary>
        public const string ATTRIBUTE_PROPERTIES = @$"^.*\(.*(Example|ErrorMessage)\s*\=\s*({STRING_LITERAL}).*$";
        /// <summary>
        /// Exit code attribute
        /// </summary>
        public const string EXIT_CODE_ATTRIBUTE = @$"^.*ExitCode[^\(]*\(\d+\s*,\s*({STRING_LITERAL}).*$";
        /// <summary>
        /// Include strings
        /// </summary>
        public const string INCLUDE_STRINGS = @$"^.*[^\@\$\""\\]({STRING_LITERAL}).*wan24I8NTool\:include.*$";
        /// <summary>
        /// Regular expression to match a string literal
        /// </summary>
        public const string STRING_LITERAL = @"\""(\\.|[^\\\""])+[^\\]\""";
        /// <summary>
        /// Replacement for <see cref="RX_NORMALIZE1"/>
        /// </summary>
        public const string NORMALIZE1_RPL = "$1";
        /// <summary>
        /// Replacelement for <see cref="RX_NORMALIZE2"/>
        /// </summary>
        public const string NORMALIZE2_RPL = "$1";
        /// <summary>
        /// Replacement for <see cref="RX_METHODS_AND_ATTRIBUTES"/>
        /// </summary>
        public const string METHODS_AND_ATTRIBUTES_RPL = "$4";
        /// <summary>
        /// Replacement for <see cref="RX_ATTRIBUTE_PROPERTIES"/>
        /// </summary>
        public const string ATTRIBUTE_PROPERTIES_RPL = "$2";
        /// <summary>
        /// Replacement for <see cref="RX_EXIT_CODE_ATTRIBUTE"/>
        /// </summary>
        public const string EXIT_CODE_ATTRIBUTE_RPL = "$1";
        /// <summary>
        /// Replacement for <see cref="RX_INCLUDE_STRINGS"/>
        /// </summary>
        public const string INCLUDE_STRINGS_RPL = "$1";

        /// <summary>
        /// Parser data
        /// </summary>
        private static readonly Dictionary<string, string> ParserData = new()
        {
            {"rxStringLiteral", STRING_LITERAL}
        };
        /// <summary>
        /// Normalize a string literal (remove characters before and maybe after)
        /// </summary>
        public static readonly Regex RX_NORMALIZE1 = NORMALIZE1_Generated();
        /// <summary>
        /// Normalize a string literal without characters before (remove characters after)
        /// </summary>
        public static readonly Regex RX_NORMALIZE2 = NORMALIZE2_Generated();
        /// <summary>
        /// Methods and attributes
        /// </summary>
        public static readonly Regex RX_METHODS_AND_ATTRIBUTES = METHODS_AND_ATTRIBUTES_Generated();
        /// <summary>
        /// Attribute properties
        /// </summary>
        public static readonly Regex RX_ATTRIBUTE_PROPERTIES = ATTRIBUTE_PROPERTIES_Generated();
        /// <summary>
        /// Exit code attribute
        /// </summary>
        public static readonly Regex RX_EXIT_CODE_ATTRIBUTE = EXIT_CODE_ATTRIBUTE_Generated();
        /// <summary>
        /// Include strings
        /// </summary>
        public static readonly Regex RX_INCLUDE_STRINGS = INCLUDE_STRINGS_Generated();

        /// <summary>
        /// Normalize a string literal (remove characters before and maybe after)
        /// </summary>
        /// <returns>Regular expression</returns>
        [GeneratedRegex(NORMALIZE1, RX_OPTIONS, RX_TIMEOUT_MS)]
        private static partial Regex NORMALIZE1_Generated();

        /// <summary>
        /// Normalize a string literal without characters before (remove characters after)
        /// </summary>
        /// <returns>Regular expression</returns>
        [GeneratedRegex(NORMALIZE2, RX_OPTIONS, RX_TIMEOUT_MS)]
        private static partial Regex NORMALIZE2_Generated();

        /// <summary>
        /// Methods and attributes
        /// </summary>
        /// <returns>Regular expression</returns>
        [GeneratedRegex(METHODS_AND_ATTRIBUTES, RX_OPTIONS, RX_TIMEOUT_MS)]
        private static partial Regex METHODS_AND_ATTRIBUTES_Generated();

        /// <summary>
        /// Attribute properties
        /// </summary>
        /// <returns>Regular expression</returns>
        [GeneratedRegex(ATTRIBUTE_PROPERTIES, RX_OPTIONS, RX_TIMEOUT_MS)]
        private static partial Regex ATTRIBUTE_PROPERTIES_Generated();

        /// <summary>
        /// Exit code attribute
        /// </summary>
        /// <returns>Regular expression</returns>
        [GeneratedRegex(EXIT_CODE_ATTRIBUTE, RX_OPTIONS, RX_TIMEOUT_MS)]
        private static partial Regex EXIT_CODE_ATTRIBUTE_Generated();

        /// <summary>
        /// Include strings
        /// </summary>
        /// <returns>Regular expression</returns>
        [GeneratedRegex(INCLUDE_STRINGS, RX_OPTIONS, RX_TIMEOUT_MS)]
        private static partial Regex INCLUDE_STRINGS_Generated();
    }
}
