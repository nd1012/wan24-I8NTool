using GetText.Loaders;
using Karambolo.PO;
using System.ComponentModel;
using wan24.CLI;
using wan24.Compression;
using wan24.Core;
using wan24.StreamSerializerExtensions;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8NTool
{
    // Build
    public sealed partial class I8NApi
    {
        /// <summary>
        /// Build an internationalization file from multiple input sources
        /// </summary>
        /// <param name="jsonInput">JSON (UTF-8) input filenames</param>
        /// <param name="poInput">PO (gettext) input filenames</param>
        /// <param name="moInput">MO (gettext) input filenames</param>
        /// <param name="output">Internationalization output filename (if not given, STDOUT will be used; existing file will be overwritten)</param>
        /// <param name="compress">To compress the internationalization file</param>
        /// <param name="json">To read JSON (UTF-8) from STDIN</param>
        /// <param name="po">To read PO (gettext) from STDIN</param>
        /// <param name="mo">To read MO (gettext) from STDIN</param>
        /// <param name="noHeader">To skip writing a header with the version number and the compression flag</param>
        /// <param name="verbose">Write verbose informations to STDERR</param>
        /// <param name="failOnExistingKey">To fail, if an existing key would be overwritten by an additional source</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [CliApi("build", IsDefault = true)]
        [DisplayText("Build i8n file")]
        [Description("Build an internationalization (i8n) file from JSON (UTF-8) and/or PO/MO (gettext) source files")]
        [StdIn("/path/to/input.(json|po|mo)")]
        [StdOut("/path/to/output.i8n")]
        public static async Task BuildAsync(

            [CliApi(Example = "/path/to/input.json")]
            [DisplayText("JSON input")]
            [Description("JSON (UTF-8) input filenames")]
            string[]? jsonInput = null,

            [CliApi(Example = "/path/to/input.po")]
            [DisplayText("PO input")]
            [Description("PO (gettext) input filenames")]
            string[]? poInput = null,

            [CliApi(Example = "/path/to/input.mo")]
            [DisplayText("MO input")]
            [Description("MO (gettext) input filenames")]
            string[]? moInput = null,

            [CliApi(Example = "/path/to/output.i8n")]
            [DisplayText("Output")]
            [Description("Internationalization output filename (if not given, STDOUT will be used; existing file will be overwritten)")]
            string? output = null,

            [CliApi]
            [DisplayText("Compress")]
            [Description("To compress the internationalization file")]
            bool compress = false,

            [CliApi]
            [DisplayText("JSON")]
            [Description("To read JSON (UTF-8) from STDIN")]
            bool json = false,

            [CliApi]
            [DisplayText("PO")]
            [Description("To read PO (gettext) from STDIN")]
            bool po = false,

            [CliApi]
            [DisplayText("MO")]
            [Description("To read MO (gettext) from STDIN")]
            bool mo = false,

            [CliApi]
            [DisplayText("No header")]
            [Description("To skip writing a header with the version number and the compression flag")]
            bool noHeader = false,

            [CliApi]
            [DisplayText("Verbose")]
            [Description("Write verbose informations to STDERR")]
            bool verbose = false,

            [CliApi]
            [DisplayText("Fail on existing key")]
            [Description("To fail, if an existing key would be overwritten by an additional source")]
            bool failOnExistingKey = false,

            CancellationToken cancellationToken = default

            )
        {
            verbose |= Trace;
            if (Trace) WriteTrace($"Creating internationalization to {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
            int stdInCnt = 0;
            if (json) stdInCnt++;
            if (po) stdInCnt++;
            if (mo) stdInCnt++;
            if (stdInCnt > 1)
                throw new InvalidOperationException($"Can't parse multiple input formats from STDIN for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
            Dictionary<string, string[]> terms = [];
            // Read JSON source files
            if (jsonInput is not null && jsonInput.Length > 0)
                foreach (string fn in jsonInput)
                {
                    if (verbose) WriteInfo($"Processing JSON source file \"{fn}\" for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    await ReadJsonSourceAsync(
                        FsHelper.CreateFileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read), 
                        fn, 
                        terms, 
                        failOnExistingKey, 
                        verbose, 
                        cancellationToken
                        ).DynamicContext();
                }
            // Read MO source files
            MoFileParser? moParser = null;
            if (moInput is not null && moInput.Length > 0)
            {
                moParser = new();
                using MemoryPoolStream ms = new();
                foreach (string fn in moInput)
                {
                    if (verbose) WriteInfo($"Processing MO source file \"{fn}\" for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    await ReadMoSourceAsync(
                        FsHelper.CreateFileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read), 
                        fn, 
                        moParser, 
                        ms, 
                        terms, 
                        failOnExistingKey, 
                        verbose,
                        cancellationToken
                        ).DynamicContext();
                }
            }
            // Read PO source files
            POParser? poParser = null;
            if (poInput is not null && poInput.Length > 0)
            {
                poParser = new();
                using MemoryPoolStream ms = new();
                foreach (string fn in poInput)
                {
                    if (verbose) WriteInfo($"Processing PO source file \"{fn}\" for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    await ReadPoSourceAsync(
                        FsHelper.CreateFileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read), 
                        fn, 
                        poParser, 
                        ms, 
                        terms, 
                        failOnExistingKey, 
                        verbose,
                        cancellationToken
                        ).DynamicContext();
                }
            }
            // Read JSON from STDIN
            if (json)
            {
                if (verbose) WriteInfo($"Processing JSON from STDIN for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                await ReadJsonSourceAsync(Console.OpenStandardInput(), fn: null, terms, failOnExistingKey, verbose, cancellationToken).DynamicContext();
            }
            // Read MO from STDIN
            if (mo)
            {
                if (verbose) WriteInfo($"Processing MO from STDIN for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                moParser ??= new();
                using MemoryPoolStream ms = new();
                await ReadMoSourceAsync(Console.OpenStandardInput(), fn: null, moParser, ms, terms, failOnExistingKey, verbose, cancellationToken).DynamicContext();
            }
            // Read PO from STDIN
            if (po)
            {
                if (verbose) WriteInfo($"Processing PO from STDIN for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                poParser ??= new();
                using MemoryPoolStream ms = new();
                await ReadPoSourceAsync(Console.OpenStandardInput(), fn: null, poParser, ms, terms, failOnExistingKey, verbose, cancellationToken).DynamicContext();
            }
            if (verbose) WriteInfo($"Found {terms.Count} terms in total{(output is null ? "STDOUT" : $"output file \"{output}\"")}");
            // Write output
            if (verbose) WriteInfo($"Writing internationalization file to {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
            Stream outputStream = output is null
                ? Console.OpenStandardOutput()
                : FsHelper.CreateFileStream(output, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, overwrite: true);
            await using (outputStream.DynamicContext())
            {
                // Header
                if (!noHeader)
                {
                    if (Trace) WriteTrace($"Writing header to {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    int header = VERSION;
                    if (compress) header |= HEADER_COMPRESSION_FLAG;
                    if (Trace) WriteTrace($"Writing header {header} (version {VERSION}, compressed: {compress}) for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    await outputStream.WriteAsync((byte)header, cancellationToken).DynamicContext();
                }
                // Body
                if (compress)
                {
                    // Compressed
                    if (Trace) WriteTrace($"Use compression \"{CompressionHelper.DefaultAlgorithm.DisplayName}\" for {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    using MemoryPoolStream ms = new();
                    await JsonHelper.EncodeAsync(terms, ms, cancellationToken: cancellationToken).DynamicContext();
                    ms.Position = 0;
                    CompressionOptions options = CompressionHelper.DefaultAlgorithm.DefaultOptions;
                    options.FlagsIncluded = true;
                    options.AlgorithmIncluded = true;
                    options.UncompressedLengthIncluded = true;
                    options.LeaveOpen = true;
                    options = await CompressionHelper.DefaultAlgorithm.WriteOptionsAsync(ms, outputStream, options, cancellationToken).DynamicContext();
                    using Stream compression = CompressionHelper.DefaultAlgorithm.GetCompressionStream(outputStream, options);
                    await ms.CopyToAsync(compression, cancellationToken).DynamicContext();
                }
                else
                {
                    // Uncompressed
                    if (Trace) WriteTrace($"Write uncompressed to {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
                    await JsonHelper.EncodeAsync(terms, outputStream, cancellationToken: cancellationToken).DynamicContext();
                }
            }
            if (verbose) WriteInfo($"Done writing internationalization to {(output is null ? "STDOUT" : $"output file \"{output}\"")}");
        }
    }
}
