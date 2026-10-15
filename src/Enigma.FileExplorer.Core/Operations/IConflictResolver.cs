using System.Threading;
using System.Threading.Tasks;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Decides what to do when a transfer's destination already exists — in the application, by asking the user.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Decides one conflict.
    /// </summary>
    /// <param name="conflict">The conflict.</param>
    /// <param name="cancellationToken">Cancels the question with the transfer.</param>
    /// <returns>The decision.</returns>
    Task<ConflictResolution> ResolveAsync(ConflictInfo conflict, CancellationToken cancellationToken);
}
