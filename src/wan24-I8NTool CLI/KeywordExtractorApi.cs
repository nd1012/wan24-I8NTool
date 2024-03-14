using Karambolo.PO;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using wan24.CLI;
using wan24.Core;
using wan24.I8NKws;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8NTool
{
    /// <summary>
    /// PO extractor CLI API
    /// </summary>
    [CliApi("extractor", IsDefault = true)]
    [DisplayText("PO extractor")]
    [Description("CLI API for creating/merging a PO or JSON file from source code extracted keywords")]
    public sealed partial class KeywordExtractorApi
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public KeywordExtractorApi() { }

        /// <summary>
        /// Fail on error?
        /// </summary>
        [CliApi("failOnError")]
        [DisplayText("Fail on error")]
        [Description("Fail the whole process on any error")]
        public static bool FailOnError { get; set; }

        /// <summary>
        /// Extract keywords from source code
        /// </summary>
        /// <param name="config">Custom configuration file</param>
        /// <param name="singleThread">Disable multi-threading?</param>
        /// <param name="verbose">Be verbose?</param>
        /// <param name="noRecursive">Disable directory recursion?</param>
        /// <param name="ext">File extensions to use (including dot; will override the configuration)</param>
        /// <param name="encoding">Source text encoding identifier</param>
        /// <param name="input">Input file-/foldernames (may be relative or absolute)</param>
        /// <param name="exclude">Excluded file-/foldernames (absolute path or filename only; will override the configuration)</param>
        /// <param name="output">Output PO filename (may be relative or absolute)</param>
        /// <param name="noHeader">Skip writing a header</param>
        /// <param name="mergeOutput">Merge the PO output to the existing PO file</param>
        /// <param name="json">Output wan24-I8NKws JSON format</param>
        /// <param name="fuzzy">Maximum old/new key distance in percent (only when merging)</param>
        [CliApi("extract", IsDefault = true)]
        [DisplayText("Extract")]
        [Description("Creates/merges a PO file with from source code extracted keywords")]
        [StdIn("/path/to/source.cs")]
        [StdOut("/path/to/target.po")]
        public static async Task ExtractAsync(

            [CliApi(Example = "/path/to/config.json")]
            [DisplayText("Configuration")]
            [Description("Path to the custom configuration JSON file to use (may be relative or absolute, or only a filename to lookup; will look in current directory, app folder and temporary folder)")]
            string? config = null,

            [CliApi]
            [DisplayText("Single threaded")]
            [Description("Disable multi-threading (process only one source file per time)")]
            bool singleThread = false,

            [CliApi]
            [DisplayText("Verbose")]
            [Description("Log processing details (to STDERR)")]
            bool verbose = false,

            [CliApi]
            [DisplayText("No recursive")]
            [Description("Don't recurse into sub-folders (top directory only)")]
            bool noRecursive = false,

            [CliApi]
            [DisplayText("Extensions")]
            [Description("File extensions to look for (including dot; will override the configuration)")]
            string[]? ext = null,

            [CliApi(Example = "UTF-8")]
            [DisplayText("Encoding")]
            [Description("Text encoding of the source files (may be any encoding (web) identifier)")]
            string? encoding = null,

            [CliApi(Example = "/path/to/source.cs")]
            [DisplayText("Source files/folders")]
            [Description("Path to source files and folders (may be relative or absolute)")]
            string[]? input = null,

            [CliApi(Example = "/path/to/source/sub/folder")]
            [DisplayText("Exclude files/folders")]
            [Description("Path to excluded source files and folders (absolute or partial path or file-/foldername only (\"*\" (any or none) and \"+\" (one or many) may be used as wildcard); case insensitive; will override the configuration)")]
            string[]? exclude = null,

            [CliApi(Example = "/path/to/output.po")]
            [DisplayText("Output path")]
            [Description("Path to the output PO file (may be relative or absolute)")]
            string? output = null,

            [CliApi]
            [DisplayText("No header")]
            [Description("Skip adding header informations to a new PO file")]
            bool noHeader = false,

            [CliApi]
            [DisplayText("Merge output")]
            [Description("Merge the PO/JSON output to the existing output PO or JSON file")]
            bool mergeOutput = false,

            [CliApi]
            [DisplayText("Output JSON")]
            [Description("To output in wan24-I8NKws JSON format")]
            bool json = false,

            [CliApi(Example = "10", ParseJson = true)]
            [DisplayText("Fuzzy factor")]
            [Description("Maximum old/new keyword distance in percent to update and mark an existing entry with the fuzzy flag (only when merging)")]
            int fuzzy = 0

            )
        {
            if (fuzzy < 0 || fuzzy > 99) throw new ArgumentOutOfRangeException(nameof(fuzzy));
            DateTime start = DateTime.Now;// Overall starting time
            // Configure
            string? configFn = null;// Finally used custom configuration filename
            if (config is not null)
            {
                // Load custom JSON configuration file
                if (Trace) WriteTrace($"Loading JSON configuration from \"{config}\"");
                if (FsHelper.FindFile(config, includeCurrentDirectory: true) is not string fn)
                    throw new FileNotFoundException("Configuration file not found", config);
                if (Trace && fn != config) WriteTrace($"Using configuration filename \"{fn}\"");
                configFn = fn;
                await AppConfig.LoadAsync<I8NToolAppConfig>(fn).DynamicContext();
            }
            if (singleThread) I8NToolConfig.SingleThread = true;// Override multithreading
            if (ext is not null && ext.Length > 0)
            {
                // Override file extensions
                if (Trace) WriteTrace($"Override file extensions with \"{string.Join(", ", ext)}\"");
                I8NToolConfig.FileExtensions.Clear();
                I8NToolConfig.FileExtensions.AddRange(ext);
            }
            if(exclude is not null && exclude.Length > 0)
            {
                // Override excludes
                if (Trace) WriteTrace($"Override excludes with \"{string.Join(" | ", exclude)}\"");
                I8NToolConfig.Exclude.Clear();
                I8NToolConfig.Exclude.AddRange(exclude);
            }
            mergeOutput |= I8NToolConfig.MergeOutput;
            if (mergeOutput)
            {
                // Merge with the existing PO output file
                if (Trace) WriteTrace("Merge with existing PO output file (if any)");
                I8NToolConfig.MergeOutput = mergeOutput;
            }
            if (encoding is not null)
            {
                // Override source encoding
                if (Trace) WriteTrace($"Override source encoding with \"{encoding}\"");
                I8NToolConfig.SourceEncoding = Encoding.GetEncoding(encoding);
            }
            if (verbose) Logging.Logger ??= new VividConsoleLogger(LogLevel.Information);// Ensure having a logger for verbose output
            if (!verbose)
            {
                // Always be verbose if a logger was configured
                if (Trace) WriteTrace($"Force verbose {Logging.Logger is not null && Info}");
                verbose = Logging.Logger is not null && Info;
            }
            if (verbose)
            {
                // Output the used final settings
                WriteInfo($"Configuration file: {configFn ?? "(none)"}");
                WriteInfo($"Multi-threading: {!I8NToolConfig.SingleThread}");
                WriteInfo($"Number of threads: {(I8NToolConfig.SingleThread ? 1 : Environment.ProcessorCount << 1)}");
                WriteInfo($"Source encoding: {I8NToolConfig.SourceEncoding.EncodingName}");
                WriteInfo($"Patterns: {I8NToolConfig.Patterns.Count}");
                WriteInfo($"Fuzzy distance: {fuzzy}");
                WriteInfo($"File extensions: {string.Join(", ", I8NToolConfig.FileExtensions)}");
                WriteInfo($"Merge to output PO file: {I8NToolConfig.MergeOutput}");
                WriteInfo($"wan24-I8NKws JSON output: {json}");
                WriteInfo($"Fail on error: {FailOnError || I8NToolConfig.FailOnError}");
            }
            if (input is not null && I8NToolConfig.FileExtensions.Count < 1) throw new InvalidDataException("Missing file extensions to look for");
            if (!I8NToolConfig.Patterns.Any(p => p.Replacement is null)) throw new InvalidDataException("Missing matching-only patterns");
            if (!I8NToolConfig.Patterns.Any(p => p.Replacement is not null)) throw new InvalidDataException("Missing replace patterns");
            // Process
            DateTime started = DateTime.Now;// Part start time
            int sources = 0;// Number of source files parsed
            long lines = 0;// Number of non-empty lines parsed
            HashSet<KeywordMatch> keywords = [];// Extracted keywords
            if (input is null)
            {
                // Use STDIN
                if (verbose) WriteInfo("Using STDIN");
                await ProcessFileAsync(Console.OpenStandardInput(), keywords, verbose).DynamicContext();
                sources++;
            }
            else
            {
                // Use given file-/foldernames
                if (verbose) WriteInfo("Using given file-/foldernames");
                if (input.Length < 1) throw new ArgumentException("Missing input locations", nameof(input));
                PathMatching excluding = new([..I8NToolConfig.Exclude]);
                ParallelFileWorker worker = new(I8NToolConfig.SingleThread ? 1 : Environment.ProcessorCount << 1, keywords, verbose)
                {
                    Name = "i8n parallel file worker"
                };
                await using (worker.DynamicContext())
                {
                    // Start the parallel worker
                    await worker.StartAsync().DynamicContext();
                    string[] extensions = [.. I8NToolConfig.FileExtensions],// File extensions to look for
                        files;// Found files in an input source folder
                    HashSet<string> filteredFiles;// Filtered files without excluded files
                    string fullPath;// Full path of the current input source
                    foreach (string path in input)
                    {
                        // Process input file-/foldernames
                        if (Trace) WriteTrace($"Handling input path \"{path}\"");
                        fullPath = Path.GetFullPath(path);
                        if (Trace && !path.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                            WriteTrace($"Full input path for \"{path}\" is \"{fullPath}\"");
                        if (Directory.Exists(fullPath))
                        {
                            // Find files in a folder (optional recursive)
                            if (excluding.IsMatch(fullPath))
                            {
                                if (verbose) WriteInfo($"Folder \"{fullPath}\" was excluded");
                                continue;
                            }
                            files = FsHelper.FindFiles(fullPath, recursive: !noRecursive, extensions: extensions).ToArray();
                            if (files.Length < 1)
                            {
                                if (verbose) WriteInfo($"Found no files in \"{fullPath}\"");
                                continue;
                            }
                            filteredFiles = new(files.Length);
                            foreach(string fn in files)
                                if (excluding.IsMatch(fn))
                                {
                                    if (verbose) WriteInfo($"File \"{fn}\" was excluded");
                                }
                                else
                                {
                                    filteredFiles.Add(fn);
                                }
                            if (filteredFiles.Count < 1)
                            {
                                if (verbose) WriteInfo($"Found no files in \"{fullPath}\" after applying exclude filters");
                                continue;
                            }
                            if (verbose) WriteInfo($"Found {filteredFiles.Count} source files in \"{fullPath}\"");
                            if (Trace)
                                foreach (string file in filteredFiles)
                                    WriteTrace($"Going to process file \"{file}\"");
                            await worker.EnqueueRangeAsync(filteredFiles).DynamicContext();
                            sources += filteredFiles.Count;
                        }
                        else if (File.Exists(fullPath))
                        {
                            // Use a given filename
                            if (excluding.IsMatch(fullPath))
                            {
                                if (verbose) WriteInfo($"File \"{fullPath}\" was excluded");
                                continue;
                            }
                            if (verbose) WriteInfo($"Add file \"{fullPath}\"");
                            await worker.EnqueueAsync(fullPath).DynamicContext();
                            sources++;
                        }
                        else
                        {
                            throw new FileNotFoundException("The given path wasn't found", fullPath);
                        }
                    }
                    // Wait until the parallel worker did finish all jobs
                    if (Trace) WriteTrace("Waiting for all files to finish processing");
                    await worker.WaitBoringAsync().DynamicContext();
                    if (worker.LastException is not null) throw new IOException("Failed to process input sources", worker.LastException);
                    lines = worker.ParsedLines;
                }
            }
            if (verbose) WriteInfo($"Done processing input source files (extracted {keywords.Count} keywords from {sources} source files with {lines} non-empty lines; took {DateTime.Now - started})");
            // Write output
            started = DateTime.Now;
            Stream? outputStream = null;// Output (file?)stream
            if (json)
            {
                // wan24-I8NKws JSON output
                KwsCatalog catalog;// KWS catalog
                if (mergeOutput && output is not null && File.Exists(output))
                {
                    // Merge to existing file
                    if (verbose) WriteInfo($"Merging results with existing wan24-I8NKws JSON file \"{output}\"");
                    if (keywords.Count < 1) throw new InvalidDataException("No keywords matched from input sources - won't touch the existing wan24-I8NKws JSON output file");
                    outputStream = FsHelper.CreateFileStream(output, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    catalog = await JsonHelper.DecodeAsync<KwsCatalog>(outputStream).DynamicContext()
                        ?? throw new InvalidDataException("Failed to load KWS catalog");
                    catalog.Validate();
                    // Merge catalog with our results
                    int newKeywords = 0,// Number of new keywords
                        existingKeywords = 0,// Number of updated keywords
                        fuzzyKeywords = 0,// Number of fuzzy logic updated keywords
                        minWeight = fuzzy > 0 ? 100 - fuzzy : 0;// Minimum weight for fuzzy keyword lookup
                    string[] catalogKeywords = [.. catalog.Keywords.Select(k => k.ID)];// Existing catalog keywords
                    KwsKeyword? entry;// A KWS entry
                    foreach (KeywordMatch match in keywords)
                    {
                        if ((entry = catalog.Keywords.FirstOrDefault(keyword => keyword.ID == match.Keyword)) is not null)
                        {
                            // Update existing entry
                            if (Trace) WriteTrace($"Keyword \"{match.KeywordLiteral}\" found at {match.Positions.Count} position(s) exists already - updating source references only");
                            entry.SourceReferences.Clear();
                            entry.SourceReferences.AddRange(match.Positions.Select(pos => new KwsSourceReference()
                            {
                                FileName = pos.FileName ?? "STDIN",
                                LineNumber = pos.LineNumber
                            }));
                            existingKeywords++;
                            continue;
                        }
                        else if (fuzzy > 0 && catalogKeywords.Length > 0 && FuzzyKeywordLookup(match.Keyword, catalogKeywords, minWeight) is string fuzzyKeyword)
                        {
                            // Fuzzy keyword update
                            if (Trace) WriteTrace($"Keyword \"{match.KeywordLiteral}\" at {match.Positions.Count} position(s) exists already (found by fuzzy matching) - updating the entry \"{fuzzyKeyword.ToLiteral()}\"");
                            entry = catalog.Keywords.First(keyword => keyword.ID == fuzzyKeyword);
                            entry.UpdateId(match.Keyword);
                            entry.SourceReferences.AddRange(match.Positions.Select(pos => new KwsSourceReference()
                            {
                                FileName = pos.FileName ?? "STDIN",
                                LineNumber = pos.LineNumber
                            }));
                            fuzzyKeywords++;
                            continue;
                        }
                        // Create new entry
                        if (Trace) WriteTrace($"Adding new keyword \"{match.KeywordLiteral}\" found at {match.Positions.Count} position(s)");
                        catalog.Keywords.Add(new KwsKeyword(match.Keyword)
                        {
                            SourceReferences = new(match.Positions.Select(pos => new KwsSourceReference()
                            {
                                FileName = pos.FileName ?? "STDIN",
                                LineNumber = pos.LineNumber
                            }))
                        });
                        newKeywords++;
                    }
                    // Handle obsolete keywords
                    int obsolete = 0;// Number of removed obsolete keywords
                    foreach (KwsKeyword obsoleteKws in catalog.Keywords.Where(entry => !keywords.Any(kw => kw.Keyword == entry.ID)).ToArray())
                    {
                        if (Trace) WriteTrace($"Marking obsolete keyword \"{obsoleteKws.IdLiteral}\"");
                        obsoleteKws.Obsolete = true;
                        obsolete++;
                    }
                    if (verbose) WriteInfo($"Merging wan24-I8NKws JSON contents done ({newKeywords} keywords added, {existingKeywords} updated, {fuzzyKeywords} fuzzy updates, {obsolete} obsolete keywords marked)");
                    // Write final PO contents
                    if (Trace) WriteTrace($"Writing new wan24-I8NKws JSON contents to the existing wan24-I8NKws JSON output file \"{output}\"");
                    outputStream.SetLength(0);
                }
                else
                {
                    // Create new file or write to STDOUT
                    if (verbose) WriteInfo($"Writing the wan24-I8NKws JSON output to {(output is null ? "STDOUT" : $"\"{output}\"")}");
                    outputStream = output is null
                        ? Console.OpenStandardOutput()
                        : FsHelper.CreateFileStream(output, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, overwrite: true);
                    catalog = new()
                    {
                        Keywords = new(keywords.Select(keyword => new KwsKeyword(keyword.Keyword)
                        {
                            SourceReferences = new(keyword.Positions.Select(pos => new KwsSourceReference()
                            {
                                FileName = pos.FileName ?? "STDIN",
                                LineNumber = pos.LineNumber
                            }))
                        }))
                    };
                    if (Trace) WriteTrace($"Writing wan24-I8NKws JSON contents to {(output is null ? "STDOUT" : $"the output wan24-I8NKws JSON file \"{output}\"")}");
                }
                await JsonHelper.EncodeAsync(catalog, outputStream, prettify: true).DynamicContext();
            }
            else
            {
                POCatalog catalog;// Final PO catalog
                MemoryPoolStream? ms = null;// Memory stream for the PO generator
                try
                {
                    if (mergeOutput && output is not null && File.Exists(output))
                    {
                        // Merge to existing PO file
                        if (verbose) WriteInfo($"Merging results with existing PO file \"{output}\"");
                        if (keywords.Count < 1) throw new InvalidDataException("No keywords matched from input sources - won't touch the existing PO output file");
                        outputStream = FsHelper.CreateFileStream(output, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        // Load existing PO contents
                        ms = new();
                        await outputStream.CopyToAsync(ms).DynamicContext();
                        ms.Position = 0;
                        POParseResult result = new POParser().Parse(ms);
                        ms.SetLength(0);
                        if (Trace || !result.Success)
                            foreach (Diagnostic diag in result.Diagnostics)
                                switch (diag.Severity)
                                {
                                    case DiagnosticSeverity.Unknown:
                                        WriteDebug($"PO parser code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    case DiagnosticSeverity.Information:
                                        WriteInfo($"PO parser information code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    case DiagnosticSeverity.Warning:
                                        WriteWarning($"PO parser warning code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    case DiagnosticSeverity.Error:
                                        WriteError($"PO parser error code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    default:
                                        WriteWarning($"PO parser {diag.Severity} code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                }
                        if (!result.Success)
                        {
                            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                            throw new InvalidDataException($"Failed to read existing PO file \"{output}\" for merging the extraction results");
                        }
                        if (verbose && !Trace)
                            foreach (Diagnostic diag in result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Unknown))
                                switch (diag.Severity)
                                {
                                    case DiagnosticSeverity.Information:
                                        WriteInfo($"PO parser information code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    case DiagnosticSeverity.Warning:
                                        WriteWarning($"PO parser warning code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    case DiagnosticSeverity.Error:
                                        WriteError($"PO parser error code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                    default:
                                        WriteWarning($"PO parser {diag.Severity} code \"{diag.Code}\", arguments {diag.Args.Length}: {diag}");
                                        break;
                                }
                        if (result.Diagnostics.HasError) FailOnErrorIfRequested();
                        catalog = result.Catalog;
                        if (string.IsNullOrWhiteSpace(catalog.Encoding))
                        {
                            if (Trace) WriteTrace("Add missing encoding to PO output");
                            catalog.Encoding = Encoding.UTF8.WebName;
                        }
                        else if (Encoding.GetEncoding(catalog.Encoding) != Encoding.UTF8)
                        {
                            WriteWarning($"PO encoding was set to \"{catalog.Encoding}\", which might cause encoding problems");
                        }
                        // Merge catalog with our results
                        int newKeywords = 0,// Number of new keywords
                            existingKeywords = 0,// Number of updated keywords
                            fuzzyKeywords = 0,// Number of fuzzy logic updated keywords
                            minWeight = fuzzy > 0 ? 100 - fuzzy : 0;// Minimum weight for fuzzy keyword lookup
                        string[] catalogKeywords = [.. catalog.Keys.Select(k => k.Id)];// Existing catalog keywords
                        POReferenceComment referencesComment;// Keyword references comment
                        POFlagsComment fuzzyFlagComment = new()// Fuzzy logic updated keyword flag comment
                        {
                            Flags = new HashSet<string>()
                        {
                            "fuzzy"
                        }
                        };
                        foreach (KeywordMatch match in keywords)
                        {
                            referencesComment = new()
                            {
                                References = new List<POSourceReference>(match.Positions.Select(p => new POSourceReference(p.FileName ?? "STDIN", p.LineNumber)))
                            };
                            if (catalog.TryGetValue(new(match.Keyword), out IPOEntry? entry))
                            {
                                // Update existing entry
                                if (Trace) WriteTrace($"Keyword \"{match.KeywordLiteral}\" found at {match.Positions.Count} position(s) exists already - updating references comment only");
                                if (entry.Comments is null)
                                {
                                    if (Trace) WriteTrace("Creating comments");
                                    entry.Comments = [referencesComment];
                                }
                                else
                                {
                                    if (entry.Comments.FirstOrDefault(c => c is POReferenceComment) is POComment referenceComment)
                                    {
                                        if (Trace) WriteTrace("Removing previous references");
                                        entry.Comments.Remove(referenceComment);
                                    }
                                    entry.Comments.Add(referencesComment);
                                }
                                existingKeywords++;
                                continue;
                            }
                            else if (fuzzy > 0 && catalogKeywords.Length > 0 && FuzzyKeywordLookup(match.Keyword, catalogKeywords, minWeight) is string fuzzyKeyword)
                            {
                                // Fuzzy keyword update
                                if (Trace) WriteTrace($"Keyword \"{match.KeywordLiteral}\" at {match.Positions.Count} position(s) exists already (found by fuzzy matching) - updating the entry \"{fuzzyKeyword.ToLiteral()}\"");
                                IPOEntry fuzzyEntry = catalog[new POKey(fuzzyKeyword)],
                                    newEntry = fuzzyEntry is POSingularEntry singular
                                        ? new POSingularEntry(new(match.Keyword))
                                        {
                                            Comments = fuzzyEntry.Comments,
                                            Translation = singular.Translation
                                        }
                                        : new POPluralEntry(new(match.Keyword), fuzzyEntry)
                                        {
                                            Comments = fuzzyEntry.Comments,
                                        };
                                if (newEntry.Comments is null)
                                {
                                    // Create comments
                                    if (Trace) WriteTrace("Creating comments");
                                    newEntry.Comments = [referencesComment, fuzzyFlagComment];
                                }
                                else if (newEntry.Comments.FirstOrDefault(c => c is POFlagsComment) is POFlagsComment flagsComment)
                                {
                                    // Add fuzzy flag
                                    if (!flagsComment.Flags.Contains("fuzzy"))
                                    {
                                        if (Trace) WriteTrace("Ading fuzzy flag");
                                        flagsComment.Flags.Add("fuzzy");
                                    }
                                }
                                else
                                {
                                    // Add fuzzy flag comment
                                    if (Trace) WriteTrace("Adding fuzzy flag comment");
                                    newEntry.Comments.Add(fuzzyFlagComment);
                                }
                                if (newEntry.Comments.FirstOrDefault(c => c is POPreviousValueComment pvc && pvc.IdKind == POIdKind.Id) is POComment pvComment)
                                {
                                    // Remove old previous value comment
                                    if (Trace) WriteTrace("Removing old previous value comment");
                                    newEntry.Comments.Remove(pvComment);
                                }
                                newEntry.Comments.Add(new POPreviousValueComment()
                                {
                                    IdKind = POIdKind.Id,
                                    Value = fuzzyEntry.Key.Id
                                });
                                // Exchange the entry
                                catalog.Remove(fuzzyEntry.Key);
                                catalog.Add(newEntry);
                                fuzzyKeywords++;
                                continue;
                            }
                            // Create new entry
                            if (Trace) WriteTrace($"Adding new keyword \"{match.KeywordLiteral}\" found at {match.Positions.Count} position(s)");
                            catalog.Add(new POSingularEntry(new(match.Keyword))
                            {
                                Comments = [referencesComment],
                                Translation = string.Empty
                            });
                            newKeywords++;
                        }
                        // Handle obsolete keywords
                        int obsolete = 0;// Number of removed obsolete keywords
                        foreach (IPOEntry entry in catalog.Values.Where(entry => !keywords.Any(kw => kw.Keyword == entry.Key.Id)).ToArray())
                        {
                            if (Trace) WriteTrace($"Removing obsolete keyword \"{entry.Key.Id.ToLiteral()}\"");
                            catalog.Remove(entry);
                            obsolete++;
                        }
                        if (verbose) WriteInfo($"Merging PO contents done ({newKeywords} keywords added, {existingKeywords} updated, {fuzzyKeywords} fuzzy updates, {obsolete} obsolete keywords removed)");
                        // Write final PO contents
                        if (Trace) WriteTrace($"Writing new PO contents to the existing PO output file \"{output}\"");
                        outputStream.SetLength(0);
                    }
                    else
                    {
                        // Create new PO file or write to STDOUT
                        if (verbose) WriteInfo($"Writing the PO output to {(output is null ? "STDOUT" : $"\"{output}\"")}");
                        outputStream = output is null
                            ? Console.OpenStandardOutput()
                            : FsHelper.CreateFileStream(output, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, overwrite: true);
                        // Entries
                        catalog = new(keywords.Select(k => new POSingularEntry(new(k.Keyword))
                        {
                            Comments = [
                                new POReferenceComment()
                            {
                                References = new List<POSourceReference>(k.Positions.Select(p => new POSourceReference(p.FileName ?? "STDIN", p.LineNumber)))
                            }
                            ],
                            Translation = string.Empty
                        }))
                        {
                            Encoding = Encoding.UTF8.WebName
                        };
                        // Header
                        if (!noHeader)
                        {
                            if (verbose) WriteInfo("Adding PO header");
                            catalog.HeaderComments = [
                                new POTranslatorComment()
                            {
                                Text = "wan24I8NTool"
                            }
                            ];
                            catalog.Headers = new Dictionary<string, string>()
                        {
                            { "Project-Id-Version", $"wan24I8NTool {Assembly.GetExecutingAssembly().GetCustomAttributeCached<AssemblyInformationalVersionAttribute>()?.InformationalVersion}" },
                            { "Report-Msgid-Bugs-To", "https://github.com/nd1012/wan24-I8NTool/issues" },
                            { "MIME-Version", "1.0" },
                            { "Content-Type", "text/plain; charset=UTF-8" },
                            { "Content-Transfer-Encoding", "8bit" },
                            { "X-Generator", $"wan24I8NTool {Assembly.GetExecutingAssembly().GetCustomAttributeCached<AssemblyInformationalVersionAttribute>()?.InformationalVersion}" }
                        };
                        }
                        // Save the PO contents
                        if (Trace) WriteTrace($"Writing PO contents to {(output is null ? "STDOUT" : $"the output PO file \"{output}\"")}");
                    }
                    // Generate PO
                    ms ??= new();
                    using (TextWriter writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true))
                        new POGenerator().Generate(writer, catalog);
                    ms.Position = 0;
                    await ms.CopyToAsync(outputStream).DynamicContext();
                    if (verbose) WriteInfo($"Done writing the PO output with {catalog.Count} entries (took {DateTime.Now - started}; total runtime {DateTime.Now - start})");
                }
                finally
                {
                    ms?.Dispose();
                    if (outputStream is not null) await outputStream.DisposeAsync().DynamicContext();
                }
            }
        }
    }
}
