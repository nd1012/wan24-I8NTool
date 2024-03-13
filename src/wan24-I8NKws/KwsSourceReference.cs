using System.ComponentModel.DataAnnotations;

namespace wan24.I8NKws
{
    /// <summary>
    /// KWS source reference
    /// </summary>
    public sealed record class KwsSourceReference
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public KwsSourceReference() { }

        /// <summary>
        /// Filename
        /// </summary>
        [MinLength(1)]
        public required string FileName { get; init; }

        /// <summary>
        /// Line number (starts with <c>1</c>)
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int LineNumber { get; init; }
    }
}
