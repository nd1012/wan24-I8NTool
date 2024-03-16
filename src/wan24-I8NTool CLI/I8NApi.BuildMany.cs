using System.ComponentModel;
using wan24.CLI;
using wan24.Core;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8NTool
{
    // Build many
    public sealed partial class I8NApi
    {
        /// <summary>
        /// Build many internationalization (i8n) files from JSON (UTF-8) and/or PO/MO (gettext) source files (output filename is the input filename with the 
        /// <c>.i8n</c> extension instead - existing files will be overwritten; default is to convert all *.json/po/mo files in the working folder)
        /// </summary>
        /// <param name="jsonInput">JSON input file (UTF-8) folder (no recursion)</param>
        /// <param name="jsonInputPattern">JSON input pattern</param>
        /// <param name="kwsInput">wan24-I8NKws JSON input file (UTF-8) folder (no recursion)</param>
        /// <param name="kwsInputPattern">wan24-I8NKws JSON input pattern</param>
        /// <param name="poInput">PO (gettext) input file folder (no recursion)</param>
        /// <param name="poInputPattern">PO input pattern</param>
        /// <param name="moInput">MO (gettext) input file folder (no recursion)</param>
        /// <param name="moInputPattern">MO input pattern</param>
        /// <param name="exclude">Path to excluded source files (absolute path or filename only ("*" (any or none) and "+" (one or many) may be used as wildcard); case insensitive)</param>
        /// <param name="compress">To compress the internationalization files</param>
        /// <param name="noHeader">To skip writing a header with the version number and the compression flag</param>
        /// <param name="singleThread">Disable multi-threading?</param>
        /// <param name="verbose">Write verbose informations to STDERR</param>
        /// <returns>Exit code</returns>
        [CliApi("buildmany")]
        [DisplayText("Build i8n files")]
        [Description("Build many internationalization (i8n) files from (wan24-I8N) JSON (UTF-8) or PO/MO (gettext) source files (output filename is the input filename with the \".i8n\" extension instead - existing files will be overwritten; default is to convert all *.json/kws/po/mo files in the working folder)")]
        [StdErr("Verbose output and errors")]
        [ExitCode(0, "Ok")]
        [ExitCode(1, "Had errors")]
        public static async Task<int> BuildManyAsync(

            [CliApi(Example = "/path/to/sources")]
            [DisplayText("JSON input")]
            [Description("JSON input file (UTF-8) folder (no recursion; default is the working folder)")]
            string jsonInput = "./",

            [CliApi(Example = "*.json")]
            [DisplayText("JSON input pattern")]
            [Description("JSON input pattern (default is \"*.json\")")]
            string jsonInputPattern = "*.json",

            [CliApi(Example = "/path/to/sources")]
            [DisplayText("wan24-I8NKws JSON input")]
            [Description("wan24-I8NKws JSON input file (UTF-8) folder (no recursion; default is the working folder)")]
            string kwsInput = "./",

            [CliApi(Example = "*.kws")]
            [DisplayText("wan24-I8NKws JSON input pattern")]
            [Description("wan24-I8NKws JSON input pattern (default is \"*.kws\")")]
            string kwsInputPattern = "*.kws",

            [CliApi(Example = "/path/to/sources")]
            [DisplayText("PO input")]
            [Description("PO (gettext) input file folder (no recursion; default is the working folder)")]
            string poInput = "./",

            [CliApi(Example = "*.po")]
            [DisplayText("PO input pattern")]
            [Description("PO (gettext) input pattern (default is \"*.po\")")]
            string poInputPattern = "*.po",

            [CliApi(Example = "/path/to/sources")]
            [DisplayText("MO input")]
            [Description("MO (gettext) input file folder (no recursion; default is the working folder)")]
            string moInput = "./",

            [CliApi(Example = "*.mo")]
            [DisplayText("MO input pattern")]
            [Description("MO input pattern (default is \"*.mo\")")]
            string moInputPattern = "*.mo",

            [CliApi(Example = "/path/to/sources/non-i8n.json")]
            [DisplayText("Exclude files")]
            [Description("Path to excluded source files (absolute path or filename only (\"*\" (any or none) and \"+\" (one or many) may be used as wildcard); case insensitive; will override the configuration)")]
            string[]? exclude = null,

            [CliApi]
            [DisplayText("Compress")]
            [Description("To compress the internationalization files")]
            bool compress = false,

            [CliApi]
            [DisplayText("No header")]
            [Description("To skip writing a header with the version number and the compression flag")]
            bool noHeader = false,

            [CliApi]
            [DisplayText("Single threaded")]
            [Description("Disable multi-threading (process only one source file per time)")]
            bool singleThread = false,

            [CliApi]
            [DisplayText("Verbose")]
            [Description("Write verbose informations to STDERR")]
            bool verbose = false

            )
        {
            verbose |= Trace;
            if (Trace) WriteTrace("Creating many internationalization files");
            if (singleThread) I8NToolConfig.SingleThread = true;// Override multithreading
            if (exclude is not null && exclude.Length > 0)
            {
                // Override excludes
                if (Trace) WriteTrace($"Override excludes with \"{string.Join(" | ", exclude)}\"");
                I8NToolConfig.Exclude.Clear();
                I8NToolConfig.Exclude.AddRange(exclude);
            }
            int threads = I8NToolConfig.SingleThread ? 1 : Environment.ProcessorCount << 1;// Number of threads to use for parallel file processing
            if (verbose)
            {
                // Output the used final settings
                WriteInfo($"JSON files: {Path.GetFullPath(jsonInput)}{(ENV.IsWindows ? '\\' : '/')}{jsonInputPattern}");
                WriteInfo($"wan24-I8NKws JSON files: {Path.GetFullPath(kwsInput)}{(ENV.IsWindows ? '\\' : '/')}{kwsInputPattern}");
                WriteInfo($"PO files: {Path.GetFullPath(poInput)}{(ENV.IsWindows ? '\\' : '/')}{poInputPattern}");
                WriteInfo($"MO files: {Path.GetFullPath(moInput)}{(ENV.IsWindows ? '\\' : '/')}{moInputPattern}");
                WriteInfo($"Header: {!noHeader}");
                WriteInfo($"Compression: {compress}");
                WriteInfo($"Multi-threading: {!I8NToolConfig.SingleThread}");
                WriteInfo($"Number of threads: {threads}");
                WriteInfo($"Fail on error: {DoFailOnError()}");
            }
            PathMatching excluding = new([.. I8NToolConfig.Exclude]);// Excluding pattern helper
            using SemaphoreSlim sync = new(threads, threads);// Task limitation (also used for synchronizing firstException)
            using CancellationTokenSource cts = new();// Cancellation on error
            Exception? firstException = null;// The last exception
            void HandleError(in string fn, in Exception ex)
            {
                // Error handling for a task
                bool fail = DoFailOnError();
                if (verbose || Trace || !fail) WriteWarning($"Processing \"{fn}\" failed: ({ex.GetType()}) {ex.Message}");
                else WriteDebug($"Processing \"{fn}\" failed: ({ex.GetType()}) {ex.Message}");
                if (firstException is null) lock (sync) firstException ??= ex;
                ErrorHandling.Handle(ex);
                if (!fail) return;
                if (Trace) WriteTrace($"Forced to fail in total after \"{fn}\" processing error");
                cts.Cancel();
            }
            try
            {
                // JSON files
                if (Directory.Exists(jsonInput))
                {
                    IEnumerable<string> files = FsHelper.FindFiles(jsonInput, searchPattern: jsonInputPattern, recursive: false);
                    if (files.Any())
                    {
                        foreach (string fn in files)
                        {
                            if (excluding.IsMatch(fn))
                            {
                                if (verbose) WriteInfo($"File \"{fn}\" was excluded");
                                continue;
                            }
                            if (Trace) WriteTrace($"Waiting for a thread for processing \"{fn}\"");
                            await sync.WaitAsync(cts.Token).DynamicContext();
                            _ = ((Func<Task>)(async () =>
                            {
                                try
                                {
                                    if (Trace) WriteTrace($"Thread processing \"{fn}\"");
                                    await BuildAsync(
                                        jsonInput: [fn],
                                        output: Path.Combine(jsonInput, $"{Path.GetFileNameWithoutExtension(fn)}.i8n"),
                                        compress: compress,
                                        noHeader: noHeader,
                                        verbose: verbose,
                                        cancellationToken: cts.Token
                                        ).DynamicContext();
                                }
                                catch (Exception ex)
                                {
                                    HandleError(fn, ex);
                                }
                                finally
                                {
                                    if (Trace) WriteTrace($"Processing \"{fn}\" done - releasing thread");
                                    sync.Release();
                                }
                            })).StartFairTask();
                        }
                    }
                    else if(verbose)
                    {
                        WriteInfo($"No JSON files found for \"{Path.GetFullPath(jsonInput)}{(ENV.IsWindows ? '\\' : '/')}{jsonInputPattern}\"");
                    }
                }
                // KWS files
                if (Directory.Exists(kwsInput))
                {
                    IEnumerable<string> files = FsHelper.FindFiles(kwsInput, searchPattern: kwsInputPattern, recursive: false);
                    if (files.Any())
                    {
                        foreach (string fn in files)
                        {
                            if (excluding.IsMatch(fn))
                            {
                                if (verbose) WriteInfo($"File \"{fn}\" was excluded");
                                continue;
                            }
                            if (Trace) WriteTrace($"Waiting for a thread for processing \"{fn}\"");
                            await sync.WaitAsync(cts.Token).DynamicContext();
                            _ = ((Func<Task>)(async () =>
                            {
                                try
                                {
                                    if (Trace) WriteTrace($"Thread processing \"{fn}\"");
                                    await BuildAsync(
                                        kwsInput: [fn],
                                        output: Path.Combine(kwsInput, $"{Path.GetFileNameWithoutExtension(fn)}.i8n"),
                                        compress: compress,
                                        noHeader: noHeader,
                                        verbose: verbose,
                                        cancellationToken: cts.Token
                                        ).DynamicContext();
                                }
                                catch (Exception ex)
                                {
                                    HandleError(fn, ex);
                                }
                                finally
                                {
                                    if (Trace) WriteTrace($"Processing \"{fn}\" done - releasing thread");
                                    sync.Release();
                                }
                            })).StartFairTask();
                        }
                    }
                    else if (verbose)
                    {
                        WriteInfo($"No wan24-I8NKws JSON files found for \"{Path.GetFullPath(kwsInput)}{(ENV.IsWindows ? '\\' : '/')}{kwsInputPattern}\"");
                    }
                }
                // PO files
                if (Directory.Exists(poInput))
                {
                    IEnumerable<string> files = FsHelper.FindFiles(poInput, searchPattern: poInputPattern, recursive: false);
                    if (files.Any())
                    {
                        foreach (string fn in files)
                        {
                            if (excluding.IsMatch(fn))
                            {
                                if (verbose) WriteInfo($"File \"{fn}\" was excluded");
                                continue;
                            }
                            if (Trace) WriteTrace($"Waiting for a thread for processing \"{fn}\"");
                            await sync.WaitAsync(cts.Token).DynamicContext();
                            _ = ((Func<Task>)(async () =>
                            {
                                try
                                {
                                    if (Trace) WriteTrace($"Thread processing \"{fn}\"");
                                    await BuildAsync(
                                        poInput: [fn],
                                        output: Path.Combine(poInput, $"{Path.GetFileNameWithoutExtension(fn)}.i8n"),
                                        compress: compress,
                                        noHeader: noHeader,
                                        verbose: verbose,
                                        cancellationToken: cts.Token
                                        ).DynamicContext();
                                }
                                catch (Exception ex)
                                {
                                    HandleError(fn, ex);
                                }
                                finally
                                {
                                    if (Trace) WriteTrace($"Processing \"{fn}\" done - releasing thread");
                                    sync.Release();
                                }
                            })).StartFairTask();
                        }
                    }
                    else if (verbose)
                    {
                        WriteInfo($"No PO files found for \"{Path.GetFullPath(poInput)}{(ENV.IsWindows ? '\\' : '/')}{poInputPattern}\"");
                    }
                }
                // MO files
                if (Directory.Exists(moInput))
                {
                    IEnumerable<string> files = FsHelper.FindFiles(moInput, searchPattern: moInputPattern, recursive: false);
                    if (files.Any())
                    {
                        foreach (string fn in files)
                        {
                            if (excluding.IsMatch(fn))
                            {
                                if (verbose) WriteInfo($"File \"{fn}\" was excluded");
                                continue;
                            }
                            if (Trace) WriteTrace($"Waiting for a thread for processing \"{fn}\"");
                            await sync.WaitAsync(cts.Token).DynamicContext();
                            _ = ((Func<Task>)(async () =>
                            {
                                try
                                {
                                    if (Trace) WriteTrace($"Thread processing \"{fn}\"");
                                    await BuildAsync(
                                        moInput: [fn],
                                        output: Path.Combine(moInput, $"{Path.GetFileNameWithoutExtension(fn)}.i8n"),
                                        compress: compress,
                                        noHeader: noHeader,
                                        verbose: verbose,
                                        cancellationToken: cts.Token
                                        ).DynamicContext();
                                }
                                catch (Exception ex)
                                {
                                    HandleError(fn, ex);
                                }
                                finally
                                {
                                    if (Trace) WriteTrace($"Processing \"{fn}\" done - releasing thread");
                                    sync.Release();
                                }
                            })).StartFairTask();
                        }
                    }
                    else if (verbose)
                    {
                        WriteInfo($"No MO files found for \"{Path.GetFullPath(moInput)}{(ENV.IsWindows ? '\\' : '/')}{moInputPattern}\"");
                    }
                }
                if (verbose) WriteInfo($"Done creating many internationalization files{(firstException is null ? string.Empty : " with errors")}");
                return firstException is null ? 0 : 1;
            }
            catch (OperationCanceledException)
            {
                if (firstException is null) throw;
                throw new AggregateException(firstException);
            }
            catch (Exception ex)
            {
                if (firstException is null) throw;
                throw new AggregateException(firstException, ex);
            }
        }
    }
}
