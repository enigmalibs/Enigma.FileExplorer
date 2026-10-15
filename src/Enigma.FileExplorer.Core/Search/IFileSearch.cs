using System.Collections.Generic;
using System.Threading;

namespace Enigma.FileExplorer.Core.Search;

/// <summary>
/// Searches below a directory, streaming hits as they are found.
/// </summary>
public interface IFileSearch
{
    /// <summary>
    /// Searches. The walk runs off the caller's thread; hits arrive in the order they are found, nearest
    /// folders first, and stop at <see cref="SearchQuery.MaxResults"/>.
    /// </summary>
    /// <param name="query">The search.</param>
    /// <param name="cancellationToken">Stops the search.</param>
    /// <returns>The hits.</returns>
    IAsyncEnumerable<SearchHit> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
