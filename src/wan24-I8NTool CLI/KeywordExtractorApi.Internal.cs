﻿using FuzzySharp;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
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
                bool replaced;// If a replace pattern did match during the replace loop
                string currentLine,// Current line in the source file (without the last matched patterns)
                    keyword;// Currently matched keyword
                int lineNumber = 0;// Current line number in the source file (starts with 1)
                KeywordMatch? match;// Existing/new Poedit parser match
                KeywordParserPattern? pattern;// First matching Poedit parser pattern (may be a matching pattern or a replacement)
                Match? rxMatch = null;// Regular expression match of the first matching Poedit parser pattern
                HashSet<int> matched = [],// Indexes of keyword matching patterns
                    replacing = [];// Indexes of keyword applied replacing patterns
                using StreamReader reader = new(stream, I8NToolConfig.SourceEncoding);// Source file reader which uses the configured source encoding
                while (await reader.ReadLineAsync().DynamicContext() is string line)
                {
                    // File contents per line loop
                    lineNumber++;
                    if (Trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber}");
                    if (line.Trim() == string.Empty)
                    {
                        if (Trace) WriteTrace($"Skipping empty source file \"{fileName}\" line #{lineNumber}");
                        continue;
                    }
                    parsedLines++;
                    currentLine = keyword = line;
                    matched.Clear();
                    replacing.Clear();
                    while (true)
                    {
                        // Current line parsing loop (parse until no Poedit parser pattern is matching)
                        pattern = I8NToolConfig.Patterns
                            .FirstOrDefault(p => p.Replacement is null && (rxMatch = p.Expression.Matches(currentLine).FirstOrDefault()) is not null);
                        if (pattern is null)
                        {
                            if (Trace) WriteTrace($"No pattern matching for source file \"{fileName}\" line #{lineNumber}");
                            break;
                        }
                        matched.Add(I8NToolConfig.Patterns.ElementIndex(pattern));
                        // Handle the current match
                        Contract.Assert(rxMatch is not null);
                        if (Trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} pattern \"{pattern.Pattern}\" matched \"{rxMatch.Groups[1].Value}\"");
                        replaced = true;
                        keyword = currentLine;
                        bool br = false, dbg = true;
                        while (replaced)
                        {
                            // Poedit parser pattern loop (replace until we have the final keyword)
                            replaced = false;
                            br = false;
                            foreach (KeywordParserPattern replace in I8NToolConfig.Patterns.Where(p => p.Replacement is not null && p.Expression.IsMatch(keyword)))
                            {
                                if (dbg && keyword.Contains("), Done == DateTime.MinValue ?"))
                                {
                                    br = true;
                                    System.Diagnostics.Debugger.Break();
                                }
                                if (Trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} replacement pattern \"{replace.Pattern}\" matched \"{keyword}\"");
                                replaced = true;
                                keyword = replace.Expression.Replace(keyword, replace.Replacement!);
                                replacing.Add(I8NToolConfig.Patterns.ElementIndex(replace));
                                if (Trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} replacement pattern \"{replace.Pattern}\" replaced to \"{keyword}\"");
                            }
                            if (dbg && (br || keyword.Contains("), Done == DateTime.MinValue ?"))) System.Diagnostics.Debugger.Break();
                        }
                        // Remove the parsed keyword from the current line and store its position
                        currentLine = currentLine.Replace(keyword, string.Empty);
                        if (Trace) WriteTrace($"Source file \"{fileName}\" line #{lineNumber} new line is \"{currentLine}\" after keyword \"{keyword}\" was extracted");
                        // Decode the parsed keyword literal to a string
                        keyword = keyword.Trim();
                        if (keyword.StartsWith('\'')) keyword = $"\"{keyword[1..]}";
                        if (keyword.EndsWith('\'')) keyword = $"{keyword[..^1]}\"";
                        if (!keyword.StartsWith('\"') || !keyword.EndsWith('\"'))
                        {
                            WriteError($"Source file \"{fileName}\" line #{lineNumber} keyword \"{keyword}\" is not a valid string literal (regular expression pattern failure)");
                            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                            FailOnErrorIfRequested();
                            continue;
                        }
                        try
                        {
                            keyword = JsonHelper.Decode<string>(keyword) ?? throw new InvalidDataException($"Failed to decode keyword \"{keyword}\"");// keyword = "message"
                        }
                        catch(Exception ex)
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
                            else if (Trace)
                            {
                                WriteTrace($"Source file \"{fileName}\" line #{lineNumber} existing keyword \"{match.KeywordLiteral}\"");
                            }
                            match.MatchingPatterns.Clear();
                            match.MatchingPatterns.AddRange(matched);
                            match.ReplacingPatterns.Clear();
                            match.ReplacingPatterns.AddRange(replacing);
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
                    .FirstOrDefault() ?? null;

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
            private readonly SemaphoreSync ParsedLinesSync = new();
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
                using SemaphoreSyncContext ssc = await ParsedLinesSync.SyncContextAsync(cancellationToken).DynamicContext();
                _ParsedLines += lines;
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                ParsedLinesSync.Dispose();
            }

            /// <inheritdoc/>
            protected override async Task DisposeCore()
            {
                await base.DisposeCore().DynamicContext();
                await ParsedLinesSync.DisposeAsync().DynamicContext();
            }
        }
    }
}
