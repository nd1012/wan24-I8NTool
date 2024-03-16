using FuzzySharp;
using wan24.Core;
using static wan24.Core.Logger;
using static wan24.Core.Logging;

namespace wan24.I8NTool
{
    // Internal
    public sealed partial class KeywordExtractorApi
    {
        /// <summary>
        /// Process a source file
        /// </summary>
        /// <param name="stream">Stream (will be disposed!)</param>
        /// <param name="keywords">Keywords</param>
        /// <param name="verbose">Be verbose?</param>
        /// <param name="fileName">Filename</param>
        /// <returns>Number of parsed non-empty lines</returns>
        private static async Task<int> ProcessFileAsync(
            Stream stream,
            HashSet<KeywordMatch> keywords,
            bool verbose,
            string? fileName = null
            )
        {
            if (verbose) WriteInfo($"Processing source file \"{fileName}\"");
            int parsedLines = 0;
            await using (stream.DynamicContext())
            {
                bool replaced,// If a replace pattern did match during the replace loop
                    trace = Trace;// If tracing
                string currentLine,// Current line in the source file (without the last matched patterns)
                    keyword;// Currently matched keyword
                int lineNumber = 0,// Current line number in the source file (starts with 1)
                    patternCount = I8NToolConfig.Patterns.Count,// Number of patterns
                    i;// Index counter
                KeywordMatch? match;// Current keyword match
                KeywordParserPattern[] patterns = [.. I8NToolConfig.Patterns];// Available patterns
                KeywordParserPattern? pattern = null,// First matching pattern (may be a matching pattern or a replacement)
                    tempPattern;// Temporary pattern variable
#if DEBUG
                HashSet<int> matched = [],// Indexes of keyword matching patterns
                    replacing = [];// Indexes of keyword applied replacing patterns
#endif
                using StreamReader reader = new(stream, I8NToolConfig.SourceEncoding, leaveOpen: true);// Source file reader which uses the configured source encoding
                Task<string?> nextLine = reader.ReadLineAsync();// Task which reads the next line to process
                try
                {
                    while (await nextLine.DynamicContext() is string line)
                    {
                        // Pre-read the next line during processing the current line
                        nextLine = reader.ReadLineAsync();
                        // File contents per line loop
                        lineNumber++;
                        if (trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber}");
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            if (trace) WriteTrace($"Skipping empty source file \"{fileName}\" line #{lineNumber}");
                            continue;
                        }
                        parsedLines++;
                        currentLine = keyword = line;
#if DEBUG
                        matched.Clear();
                        replacing.Clear();
#endif
                        while (true)
                        {
                            // Current line parsing loop (parse until no pattern is matching)
                            pattern = null;
                            for (i = 0; i < patternCount; i++)
                            {
                                tempPattern = patterns[i];
                                if (tempPattern.ReplaceOnly || !tempPattern.Expression.IsMatch(currentLine)) continue;
                                pattern = tempPattern;
                                break;
                            }
                            if (pattern is null)
                            {
                                if (trace) WriteTrace($"No pattern matching for source file \"{fileName}\" line #{lineNumber}");
                                break;
                            }
#if DEBUG
                            matched.Add(i);
#endif
                            // Handle the current match
                            if (trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} matched by pattern \"{pattern.Pattern}\"");
                            replaced = true;
                            if (pattern.Replacement is null)
                            {
                                keyword = currentLine;
                            }
                            else
                            {
                                keyword = pattern.Expression.Replace(currentLine, pattern.Replacement);
                                if (trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} matching pattern \"{pattern.Pattern}\" replaced to \"{keyword.ToLiteral()}\"");
                            }
                            while (replaced)
                            {
                                // Parser pattern loop (replace until we have the final keyword)
                                replaced = false;
                                for (i = 0; i < patternCount; i++)
                                {
                                    tempPattern = patterns[i];
                                    if (tempPattern == pattern || !tempPattern.ReplaceOnly || !tempPattern.Expression.IsMatch(keyword)) continue;
                                    if (trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} replacement pattern \"{tempPattern.Pattern}\" matched \"{keyword.ToLiteral()}\"");
                                    replaced = true;
                                    keyword = tempPattern.Expression.Replace(keyword, tempPattern.Replacement!);
#if DEBUG
                                    replacing.Add(i);
#endif
                                    if (trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} replacement pattern \"{tempPattern.Pattern}\" replaced to \"{keyword.ToLiteral()}\"");
                                }
                            }
                            // Remove the parsed keyword from the current line and store its position
                            if (keyword.Length < 1)
                            {
                                WriteError($"Empty keyword matched in source file \"{fileName}\" line #{lineNumber} - skip parsing the rest of the line");
                                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                                FailOnErrorIfRequested();
                                break;
                            }
                            currentLine = currentLine.Replace(keyword, string.Empty);
                            if (trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} new line is \"{currentLine}\" after keyword \"{keyword.ToLiteral()}\" was extracted");
                            // Decode the parsed keyword literal to a string
                            keyword = keyword.Trim();
                            if (keyword.StartsWith('\'')) keyword = $"\"{keyword[1..]}";
                            if (keyword.EndsWith('\'')) keyword = $"{keyword[..^1]}\"";
                            if (!keyword.StartsWith('\"') || !keyword.EndsWith('\"'))
                            {
                                WriteError($"Source file \"{fileName}\" line #{lineNumber} keyword \"{keyword.ToLiteral()}\" is not a valid string literal (regular expression pattern failure)");
                                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                                FailOnErrorIfRequested();
                                continue;
                            }
                            try
                            {
                                keyword = JsonHelper.Decode<string>(keyword) ?? throw new InvalidDataException($"Failed to decode keyword \"{keyword}\"");// keyword = "message"
                            }
                            catch (Exception ex)
                            {
                                WriteError($"Source file \"{fileName}\" line #{lineNumber} keyword \"{keyword.ToLiteral()}\" failed to decode to string: ({ex.GetType()}) {ex.Message}");
                                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                                FailOnErrorIfRequested();
                                continue;
                            }
                            // Store the parsed keyword (position)
                            lock (keywords)
                            {
                                match = keywords.FirstOrDefault(m => m.Keyword == keyword);
                                if (match is null)
                                {
                                    if (Trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} new keyword \"{keyword.ToLiteral()}\"");
                                    keywords.Add(match = new()
                                    {
                                        Keyword = keyword
                                    });
                                }
                                else if (trace)
                                {
                                    WriteTrace($"Source file \"{fileName}\" line #{lineNumber} existing keyword \"{match.KeywordLiteral}\"");
                                }
#if DEBUG
                                match.MatchingPatterns.Clear();
                                match.MatchingPatterns.AddRange(matched);
                                match.ReplacingPatterns.Clear();
                                match.ReplacingPatterns.AddRange(replacing);
#endif
                                match.Positions.Add(new()
                                {
                                    FileName = fileName,
                                    LineNumber = lineNumber
                                });
                            }
                            if (verbose) WriteInfo($"Found keyword \"{match.KeywordLiteral}\" in source {(fileName is null ? string.Empty : $" file \"{fileName}\"")} on line #{lineNumber}");
                        }
                    }
                }
                finally
                {
                    if(!nextLine.IsCompleted)
                        try
                        {
                            await nextLine.DynamicContext();
                        }
                        catch
                        {
                        }
                }
            }
            return parsedLines;
        }

        /// <summary>
        /// Fail on error, if requested
        /// </summary>
        private static void FailOnErrorIfRequested()
        {
            if (FailOnError || I8NToolConfig.FailOnError)
                throw new InvalidDataException("Forced to fail in total on any error");
        }

        /// <summary>
        /// Fuzzy keyword lookup
        /// </summary>
        /// <param name="newKeyword">New keyword</param>
        /// <param name="existingKeywords">Existing keywords</param>
        /// <param name="minWeight">Minimum weight in percent</param>
        /// <returns>Best matching existing keyword</returns>
        private static string? FuzzyKeywordLookup(
            string newKeyword,
            in IEnumerable<string> existingKeywords,
            int minWeight
            )
            => newKeyword.Length < 1
                ? null
                : existingKeywords
                    .Where(keyword => keyword.Length > 0)
                    .Select(keyword => (keyword, Fuzz.WeightedRatio(newKeyword, keyword)))
                    .Where(info => info.Item2 <= minWeight)
                    .OrderBy(info => info.Item2)
                    .Select(info => info.keyword)
                    .FirstOrDefault();

        /// <summary>
        /// File worker
        /// </summary>
        /// <remarks>
        /// Constructor
        /// </remarks>
        /// <param name="capacity">Capacity</param>
        /// <param name="keywords">Keywords</param>
        /// <param name="verbose">Be verbose?</param>
        private sealed class ParallelFileWorker(in int capacity, in HashSet<KeywordMatch> keywords, in bool verbose)
            : ParallelItemQueueWorkerBase<string>(capacity, threads: capacity)
        {
            /// <summary>
            /// Thread synchronization
            /// </summary>
            private readonly object ParsedLinesSync = new();
            /// <summary>
            /// Total number of parsed non-empty lines from processed files
            /// </summary>
            private long _ParsedLines = 0;

            /// <summary>
            /// Keywords
            /// </summary>
            public HashSet<KeywordMatch> Keywords { get; } = keywords;

            /// <summary>
            /// Verbose?
            /// </summary>
            public bool Verbose { get; } = verbose;

            /// <summary>
            /// Total number of parsed non-empty lines from processed files
            /// </summary>
            public long ParsedLines => _ParsedLines;

            /// <inheritdoc/>
            protected override async Task ProcessItem(string item, CancellationToken cancellationToken)
            {
                if (Trace) WriteTrace($"Now going to process source file \"{item}\"");
                long lines;
                FileStream fs = FsHelper.CreateFileStream(item, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using (fs.DynamicContext())
                     lines = await ProcessFileAsync(fs, Keywords, Verbose, item).DynamicContext();
                lock (ParsedLinesSync) _ParsedLines += lines;
            }
        }
    }
}
