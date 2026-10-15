namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// A search below a directory.
/// </summary>
/// <param name="Root">Where the search starts; everything below it is searched.</param>
/// <param name="Pattern">A name pattern (<c>*</c> and <c>?</c> wildcards, else a substring) or the text to find.</param>
/// <param name="Kind">Names or contents.</param>
/// <param name="IncludeHidden">Whether hidden entries (and what is inside hidden folders) are searched.</param>
/// <param name="MaxResults">The most hits returned.</param>
public sealed record SearchQuery(string Root, string Pattern, SearchKind Kind, bool IncludeHidden = false, int MaxResults = SearchQuery.DefaultMaxResults)
{
    /// <summary>
    /// The default cap on hits: enough for any search a person reads, small enough to stay responsive.
    /// </summary>
    public const int DefaultMaxResults = 10_000;
}
