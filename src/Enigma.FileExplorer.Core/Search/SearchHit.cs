using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// One result of a search.
/// </summary>
/// <param name="Entry">The entry found.</param>
/// <param name="RelativeDirectory">Its folder, relative to the search root; empty for the root itself.</param>
/// <param name="LineNumber">For a content hit, the line it is on (from 1).</param>
/// <param name="LineText">For a content hit, the line, trimmed and shortened around the match.</param>
public sealed record SearchHit(FileSystemEntry Entry, string RelativeDirectory, int? LineNumber = null, string? LineText = null);
