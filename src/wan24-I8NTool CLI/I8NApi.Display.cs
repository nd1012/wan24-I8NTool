using System.ComponentModel;
using wan24.CLI;
using wan24.Compression;
using wan24.Core;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8NTool
{
    // Display
    public sealed partial class I8NApi
    {
        /// <summary>
        /// Display internationalization file informations
        /// </summary>
        /// <param name="input">Internationalization input filename (if not given, STDIN will be used)</param>
        /// <param name="uncompress">To uncompress the internationalization file (not required to use, if a header will be red)</param>
        /// <param name="noHeader">To skip reading a header with the version number and the compression flag</param>
        [CliApi("display")]
        [DisplayText("Display i8n file")]
        [Description("Display internationalization (i8n) file informations")]
        [StdIn("/path/to/input.i8n")]
        [StdErr("Output and errors")]
        public static async Task DisplayAsync(

            [CliApi(Example = "/path/to/input.i8n")]
            [DisplayText("Input")]
            [Description("Internationalization input filename (if not given, STDIN will be used)")]
            string? input = null,

            [CliApi]
            [DisplayText("Uncompress")]
            [Description("To uncompress the internationalization file (not required to use, if a header will be red)")]
            bool uncompress = false,

            [CliApi]
            [DisplayText("No header")]
            [Description("To skip reading a header with the version number and the compression flag")]
            bool noHeader = false

            )
        {
            if (Trace) WriteTrace("Displaying internationalization file");
            // Read internationalization input
            Stream inputStream = input is null
                ? Console.OpenStandardInput()
                : FsHelper.CreateFileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            (int version, CompressionOptions? options, Dictionary<string, string[]> terms) = await ReadI8NAsync(inputStream, noHeader, uncompress, Trace).DynamicContext();
            WriteInfo($"Header: {!noHeader}");
            WriteInfo($"Version: {version}");
            WriteInfo($"Compression: {(options is null ? "not compressed" : CompressionHelper.GetAlgorithm(options.Algorithm!).DisplayName)}");
            WriteInfo($"Uncompressed length: {(options is null ? "not compressed" : $"{options.UncompressedDataLength} bytes")}");
            WriteInfo($"Total terms: {terms.Count}");
            WriteInfo($"Singular terms: {terms.Values.Count(v => v.Length < 2)}");
            WriteInfo($"Plural terms: {terms.Values.Count(v => v.Length > 1)}");
            WriteInfo($"Plural counts: {string.Join(", ", terms.Values.Where(v => v.Length > 1).Select(v => v.Length.ToString()).Distinct())}");
            int missing = terms.Values.Count(v => v.Length < 1 || !v.Any(vv => vv.Length > 0));
            WriteInfo($"Missing translations: {missing}");
            if (missing > 0)
            {
                WriteInfo("Missing translation keys:");
                foreach (string key in terms.Where(kvp => kvp.Value.Length < 1 || !kvp.Value.Any(v => v.Length > 0)).Select(kvp => kvp.Key))
                    WriteInfo(key.ToQuotedLiteral());
            }
        }
    }
}
