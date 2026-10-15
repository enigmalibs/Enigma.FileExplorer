using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records the error reports shown.
/// </summary>
public sealed class FakeTransferErrorReport : ITransferErrorReport
{
    /// <summary>
    /// Every report: its title and its errors.
    /// </summary>
    public List<(string Title, IReadOnlyList<TransferError> Errors)> Shown { get; } = [];

    /// <inheritdoc />
    public Task ShowAsync(string title, IReadOnlyList<TransferError> errors)
    {
        Shown.Add((title, errors));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Answers every conflict the same way.
/// </summary>
public sealed class FixedConflictResolver : IConflictResolver
{
    /// <inheritdoc />
    public Task<ConflictResolution> ResolveAsync(ConflictInfo conflict, System.Threading.CancellationToken cancellationToken)
        => Task.FromResult(new ConflictResolution(ConflictChoice.Skip));
}
