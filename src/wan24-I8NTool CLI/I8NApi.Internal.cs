using GetText.Loaders;
using Karambolo.PO;
using wan24.Compression;
using wan24.Core;
using wan24.I8NKws;
using wan24.StreamSerializerExtensions;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8NTool
{
    // Internals
    public sealed partial class I8NApi
    {
        /// <summary>
        /// Version number
        /// </summary>
        private const int VERSION = 1;

        /// <summary>
        /// Read a JSON dictionary from a JSON source
        /// </summary>
        /// <param name="source">Source (will be disposed)</param>
        /// <param name="fn">Filename</param>
        /// <param name="terms">Terms</param>
        /// <param name="failOnExistingKey">To fail, if an existing key would be overwritten by an additional source</param>
        /// <param name="verbose">Verbose</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private static async Task ReadJsonSourceAsync(
            Stream source, 
            string? fn, 
            Dictionary<string, string[]> terms, 
            bool failOnExistingKey, 
            bool verbose,
            CancellationToken cancellationToken = default
            )
        {
            int newTerms = 0,
                overwrittenTerms = 0;
            await using (source.DynamicContext())
            {
                Dictionary<string, string[]> t = await JsonHelper.DecodeAsync<Dictionary<string, string[]>>(source, cancellationToken).DynamicContext()
                    ?? throw new InvalidDataException($"Failed to read JSON dictionary from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                if (verbose) WriteInfo($"Found {t.Count} terms");
                foreach (string key in t.Keys)
                {
                    if (terms.ContainsKey(key))
                    {
                        if (failOnExistingKey)
                            throw new InvalidDataException($"Won't overwrite existing key \"{key.ToLiteral()}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                        if (verbose) WriteInfo($"Overwriting existing key \"{key.ToLiteral()}\"");
                        overwrittenTerms++;
                    }
                    else
                    {
                        newTerms++;
                    }
                    terms[key] = t[key];
                }
            }
            if (verbose) WriteInfo($"Added {newTerms} new terms, {overwrittenTerms} overwritten");
        }

        /// <summary>
        /// Read a wan24-I8NKws JSON source
        /// </summary>
        /// <param name="source">Source (will be disposed)</param>
        /// <param name="fn">Filename</param>
        /// <param name="terms">Terms</param>
        /// <param name="failOnExistingKey">To fail, if an existing key would be overwritten by an additional source</param>
        /// <param name="verbose">Verbose</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private static async Task ReadKwsSourceAsync(
            Stream source,
            string? fn,
            Dictionary<string, string[]> terms,
            bool failOnExistingKey,
            bool verbose,
            CancellationToken cancellationToken = default
            )
        {
            int newTerms = 0,
                overwrittenTerms = 0;
            await using (source.DynamicContext())
            {
                KwsCatalog catalog = await JsonHelper.DecodeAsync<KwsCatalog>(source, cancellationToken).DynamicContext()
                    ?? throw new InvalidDataException($"Failed to read wan24-I8NKws JSON from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                catalog.Validate();
                if (verbose) WriteInfo($"Found {catalog.Keywords.Count} terms");
                foreach (KwsKeyword keyword in catalog.NonObsoleteKeywords)
                {
                    if (terms.ContainsKey(keyword.ID))
                    {
                        if (failOnExistingKey)
                            throw new InvalidDataException($"Won't overwrite existing key \"{keyword.IdLiteral}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                        if (verbose) WriteInfo($"Overwriting existing key \"{keyword.IdLiteral}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                        overwrittenTerms++;
                    }
                    else
                    {
                        newTerms++;
                    }
                    terms[keyword.ID] = [.. keyword.Translations];
                }
            }
            if (verbose) WriteInfo($"Added {newTerms} new terms, {overwrittenTerms} overwritten from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
        }

        /// <summary>
        /// Read PO from a PO source
        /// </summary>
        /// <param name="source">Source (will be disposed)</param>
        /// <param name="fn">Filename</param>
        /// <param name="parser">PO parser</param>
        /// <param name="ms">Memory stream</param>
        /// <param name="terms">Terms</param>
        /// <param name="failOnExistingKey">To fail, if an existing key would be overwritten by an additional source</param>
        /// <param name="verbose">Verbose</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private static async Task ReadPoSourceAsync(
            Stream source,
            string? fn,
            POParser parser,
            MemoryPoolStream ms,
            Dictionary<string, string[]> terms,
            bool failOnExistingKey,
            bool verbose,
            CancellationToken cancellationToken = default
            )
        {
            POParseResult result;
            int newTerms = 0,
                overwrittenTerms = 0;
            await using (source.DynamicContext())
            {
                await source.CopyToAsync(ms, cancellationToken).DynamicContext();
                ms.Position = 0;
                result = parser.Parse(ms);
                ms.SetLength(0);
            }
            if (Trace || !result.Success)
                foreach (Diagnostic diag in result.Diagnostics)
                    switch (diag.Severity)
                    {
                        case DiagnosticSeverity.Unknown:
                            WriteDebug($"PO parser code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        case DiagnosticSeverity.Information:
                            WriteInfo($"PO parser information code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        case DiagnosticSeverity.Warning:
                            WriteWarning($"PO parser warning code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        case DiagnosticSeverity.Error:
                            WriteError($"PO parser error code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        default:
                            WriteWarning($"PO parser {diag.Severity} code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                    }
            if (!result.Success)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new InvalidDataException($"Failed to read PO from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
            }
            if (verbose && !Trace)
                foreach (Diagnostic diag in result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Unknown))
                    switch (diag.Severity)
                    {
                        case DiagnosticSeverity.Information:
                            WriteInfo($"PO parser information code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        case DiagnosticSeverity.Warning:
                            WriteWarning($"PO parser warning code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        case DiagnosticSeverity.Error:
                            WriteError($"PO parser error code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                        default:
                            WriteWarning($"PO parser {diag.Severity} code \"{diag.Code}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}, arguments {diag.Args.Length}: {diag}");
                            break;
                    }
            if (verbose) WriteInfo($"Found {result.Catalog.Count} terms in {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
            foreach (IPOEntry entry in result.Catalog)
            {
                if (terms.ContainsKey(entry.Key.Id))
                {
                    if (failOnExistingKey)
                        throw new InvalidDataException($"Won't overwrite existing key \"{entry.Key.Id.ToLiteral()}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                    if (verbose) WriteInfo($"Overwriting existing key \"{entry.Key.Id.ToLiteral()}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                    overwrittenTerms++;
                }
                else
                {
                    newTerms++;
                }
                terms[entry.Key.Id] = entry switch
                {
                    POSingularEntry singular => [singular.Translation],
                    POPluralEntry plural => [.. plural],
                    _ => throw new NotImplementedException($"PO entry \"{entry.Key.Id.ToLiteral()}\" type {entry.GetType()} can't be red from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}")
                };
            }
            if (verbose) WriteInfo($"Added {newTerms} new terms, {overwrittenTerms} overwritten from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
        }

        /// <summary>
        /// Read MO from a MO source
        /// </summary>
        /// <param name="source">Source (will be disposed)</param>
        /// <param name="fn">Filename</param>
        /// <param name="parser">MO parser</param>
        /// <param name="ms">Memory stream</param>
        /// <param name="terms">Terms</param>
        /// <param name="failOnExistingKey">To fail, if an existing key would be overwritten by an additional source</param>
        /// <param name="verbose">Verbose</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private static async Task ReadMoSourceAsync(
            Stream source,
            string? fn,
            MoFileParser parser,
            MemoryPoolStream ms,
            Dictionary<string, string[]> terms,
            bool failOnExistingKey,
            bool verbose,
            CancellationToken cancellationToken = default
            )
        {
            MoFile mo;
            int newTerms = 0,
                overwrittenTerms = 0;
            await using (source.DynamicContext())
            {
                await source.CopyToAsync(ms, cancellationToken).DynamicContext();
                ms.Position = 0;
                mo = parser.Parse(ms);
                ms.SetLength(0);
            }
            if (verbose) WriteInfo($"Found {mo.Translations.Count} terms in {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
            foreach (var kvp in mo.Translations)
            {
                if (terms.ContainsKey(kvp.Key))
                {
                    if (failOnExistingKey)
                        throw new InvalidDataException($"Won't overwrite existing key \"{kvp.Key.ToLiteral()}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                    if (verbose) WriteInfo($"Overwriting existing key \"{kvp.Key.ToLiteral()}\" from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
                    overwrittenTerms++;
                }
                else
                {
                    newTerms++;
                }
                terms[kvp.Key] = kvp.Value;
            }
            if (verbose) WriteInfo($"Added {newTerms} new terms, {overwrittenTerms} overwritten from {(fn is null ? "STDIN" : $"source file \"{fn}\"")}");
        }

        /// <summary>
        /// Read an internationalization file
        /// </summary>
        /// <param name="source">Source (will be disposed)</param>
        /// <param name="noHeader">Skip heder reading</param>
        /// <param name="uncompress">Uncompress</param>
        /// <param name="verbose">Verbose</param>
        /// <returns>Version number, the used compression options and the red terms</returns>
        private static async Task<(int Version, CompressionOptions? Compression, Dictionary<string, string[]> Terms)> ReadI8NAsync(
            Stream source,
            bool noHeader,
            bool uncompress,
            bool verbose
            )
        {
            await using (source.DynamicContext())
            {
                // Header
                int version;
                bool compressed;
                if (!noHeader)
                {
                    // Require header
                    if (Trace) WriteTrace("Reading header");
                    byte header = await source.ReadOneByteAsync().DynamicContext();
                    compressed = (header & HEADER_COMPRESSION_FLAG) == HEADER_COMPRESSION_FLAG;
                    version = header & ~HEADER_COMPRESSION_FLAG;
                    if (Trace) WriteTrace($"Red header {header} (version {version}, compressed: {compressed})");
                    if (version > VERSION) throw new InvalidDataException($"Can't read file version #{version} (compressed: {compressed})");
                    if (uncompress && !compressed)
                    {
                        WriteWarning("Input source wasn't compressed - can't uncompress");
                        FailOnErrorIfRequested();
                    }
                }
                else
                {
                    // Skip header
                    if (Trace) WriteTrace("Skip reading header");
                    version = VERSION;
                    compressed = uncompress;
                }
                // Body
                CompressionOptions? options = null;
                Dictionary<string, string[]> res;
                if (compressed)
                {
                    // Compressed
                    if (Trace) WriteTrace("Decompress the input source");
                    using MemoryPoolStream ms = new();
                    options = CompressionHelper.GetDefaultOptions();
                    options.FlagsIncluded = true;
                    options.AlgorithmIncluded = true;
                    options.UncompressedLengthIncluded = true;
                    options.LeaveOpen = true;
                    options = await CompressionHelper.ReadOptionsAsync(source, ms, options).DynamicContext();
                    if (Trace)
                    {
                        WriteTrace($"Compression algorithm: {options.Algorithm}");
                        WriteTrace($"Uncompressed length: {options.UncompressedDataLength}");
                    }
                    using Stream decompression = CompressionHelper.GetDecompressionStream(source, options);
                    if (Trace) WriteTrace("Deserialize the compressed input source");
                    res = await JsonHelper.DecodeAsync<Dictionary<string, string[]>>(decompression).DynamicContext()
                        ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from input source");
                }
                else
                {
                    // Uncompressed
                    if (Trace) WriteTrace("Deserialize the uncompressed input source");
                    res = await JsonHelper.DecodeAsync<Dictionary<string, string[]>>(source).DynamicContext()
                        ?? throw new InvalidDataException("Failed to deserialize JSON dictionary from input source");
                }
                if (verbose) WriteInfo($"Red internationalization file (version {version}, compressed: {compressed}) with {res.Count} terms");
                return (version, options, res);
            }
        }

        /// <summary>
        /// Determine if to fail the whole process on any error
        /// </summary>
        /// <returns>If to fail on any error</returns>
        private static bool DoFailOnError() => FailOnError || I8NToolConfig.FailOnError;

        /// <summary>
        /// Fail on error, if requested
        /// </summary>
        private static void FailOnErrorIfRequested()
        {
            if (DoFailOnError()) throw new InvalidDataException("Forced to fail in total on any error");
        }
    }
}
