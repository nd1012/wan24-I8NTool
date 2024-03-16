using wan24.CLI;
using wan24.Core;

namespace wan24.I8NTool
{
    /// <summary>
    /// i8n tool app configuration
    /// </summary>
    public sealed class I8NToolAppConfig : AppConfigBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public I8NToolAppConfig() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="setApplied">Set as the applied configuration?</param>
        public I8NToolAppConfig(in bool setApplied) : base() => SetApplied = setApplied;

        /// <summary>
        /// Applied i8n tool app configuration
        /// </summary>
        public static I8NToolAppConfig? AppliedI8NConfig { get; private set; }

        /// <summary>
        /// Apply a configuration from the CLI configuration
        /// </summary>
        [CliConfig]
        public static string ApplyConfig
        {
            set
            {
                using FileStream fs = FsHelper.CreateFileStream(value, FileMode.Open, FileAccess.Read, FileShare.Read);
                I8NToolAppConfig config = JsonHelper.Decode<I8NToolAppConfig>(fs)
                    ?? throw new InvalidDataException($"Failed to decode parser app config from file \"{fs}\"");
                config.Apply();
            }
        }

        /// <summary>
        /// Core app configuration
        /// </summary>
        public AppConfig? Core { get; set; }

        /// <summary>
        /// CLI app configuration
        /// </summary>
        public CliAppConfig? CLI { get; set; }

        /// <summary>
        /// <see langword="true"/> to disable multi-threading (process only one source file per time)
        /// </summary>
        public bool SingleThread { get; set; }

        /// <summary>
        /// Text encoding of the source files (may be any encoding (web) identifier)
        /// </summary>
        public string? Encoding { get; set; }

        /// <summary>
        /// Custom search(/replace) regular expression patterns
        /// </summary>
        public KeywordParserPattern[]? Patterns { get; set; }

        /// <summary>
        /// File extensions to look for (including dot)
        /// </summary>
        public string[]? FileExtensions { get; set; }

        /// <summary>
        /// Path to excluded source files (absolute path or filename only (\"*\" (any or none) and \"+\" (one or many) may be used as wildcard); case insensitive)
        /// </summary>
        public static string[]? Exclude { get; }

        /// <summary>
        /// Merge the PO contents with an existing output PO file?
        /// </summary>
        public bool MergeOutput { get; set; }

        /// <summary>
        /// Fail the whole process on any error?
        /// </summary>
        public bool FailOnError { get; set; }

        /// <summary>
        /// Merge this configuration with the default configuration?
        /// </summary>
        public bool Merge { get; set; }

        /// <inheritdoc/>
        public override void Apply()
        {
            if (SetApplied)
            {
                if (AppliedI8NConfig is not null) throw new InvalidOperationException();
                AppliedI8NConfig = this;
            }
            Core?.Apply();
            CLI?.Apply();
            I8NToolConfig.SingleThread = SingleThread;
            if (Encoding is not null) I8NToolConfig.SourceEncoding = System.Text.Encoding.GetEncoding(Encoding);
            if (Patterns is not null)
            {
                if (!Merge) I8NToolConfig.Patterns.Clear();
                I8NToolConfig.Patterns.AddRange(Patterns);
            }
            if (FileExtensions is not null)
            {
                if (!Merge) I8NToolConfig.FileExtensions.Clear();
                I8NToolConfig.FileExtensions.AddRange(FileExtensions);
            }
            if(Exclude is not null)
            {
                if (!Merge) I8NToolConfig.Exclude.Clear();
                I8NToolConfig.Exclude.AddRange(Exclude);
            }
            I8NToolConfig.MergeOutput = MergeOutput;
            I8NToolConfig.FailOnError = FailOnError;
        }

        /// <inheritdoc/>
        public override async Task ApplyAsync(CancellationToken cancellationToken = default)
        {
            if (SetApplied)
            {
                if (AppliedI8NConfig is not null) throw new InvalidOperationException();
                AppliedI8NConfig = this;
            }
            if (Core is not null) await Core.ApplyAsync(cancellationToken).DynamicContext();
            if (CLI is not null) await CLI.ApplyAsync(cancellationToken).DynamicContext();
            I8NToolConfig.SingleThread = SingleThread;
            if (Encoding is not null) I8NToolConfig.SourceEncoding = System.Text.Encoding.GetEncoding(Encoding);
            if (Patterns is not null)
            {
                if (!Merge) I8NToolConfig.Patterns.Clear();
                I8NToolConfig.Patterns.AddRange(Patterns);
            }
            if (FileExtensions is not null)
            {
                if (!Merge) I8NToolConfig.FileExtensions.Clear();
                I8NToolConfig.FileExtensions.AddRange(FileExtensions);
            }
            if (Exclude is not null)
            {
                if (!Merge) I8NToolConfig.Exclude.Clear();
                I8NToolConfig.Exclude.AddRange(Exclude);
            }
            I8NToolConfig.MergeOutput = MergeOutput;
            I8NToolConfig.FailOnError = FailOnError;
        }
    }
}
