using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Search;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A search that yields scripted hits, optionally waiting on a gate before the last part so a test can
/// observe a search in flight.
/// </summary>
public sealed class FakeFileSearch : IFileSearch
{
    /// <summary>
    /// The hits every search yields, in order.
    /// </summary>
    public List<SearchHit> Hits { get; } = [];

    /// <summary>
    /// Every query run, in order.
    /// </summary>
    public List<SearchQuery> Queries { get; } = [];

    /// <summary>
    /// When set, the search yields the first hit, then waits for the gate before yielding the rest.
    /// </summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <inheritdoc />
    public async IAsyncEnumerable<SearchHit> SearchAsync(SearchQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Queries.Add(query);

        for (int i = 0; i < Hits.Count; i++)
        {
            if ((i == 1) && (Gate is { } gate))
            {
                await gate.Task.WaitAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            yield return Hits[i];
        }
    }
}
