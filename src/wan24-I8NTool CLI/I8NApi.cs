using System.ComponentModel;
using wan24.CLI;
using wan24.Core;

namespace wan24.I8NTool
{
    /// <summary>
    /// i8n API
    /// </summary>
    [CliApi("i8n")]
    [DisplayText("Internationalization")]
    [Description("This API allows (de)serializing internationalization informations from/to i8n files")]
    public sealed partial class I8NApi
    {
        /// <summary>
        /// i8n file structure header byte compression flag (bit 8)
        /// </summary>
        public const int HEADER_COMPRESSION_FLAG = 128;

        /// <summary>
        /// Constructor
        /// </summary>
        public I8NApi() { }

        /// <summary>
        /// Fail on error?
        /// </summary>
        [CliApi("failOnError")]
        [DisplayText("Fail on error")]
        [Description("Fail the whole process on any error")]
        public static bool FailOnError { get; set; }
    }
}
