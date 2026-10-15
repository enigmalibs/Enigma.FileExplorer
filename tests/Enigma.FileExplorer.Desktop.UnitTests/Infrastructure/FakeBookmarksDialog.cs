using System.Threading.Tasks;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A bookmarks dialog that answers what the test chose.
/// </summary>
public sealed class FakeBookmarksDialog : IBookmarksDialogService
{
    /// <summary>
    /// The bookmark the next showing picks; <see langword="null"/> closes without one.
    /// </summary>
    public Bookmark? Answer { get; set; }

    /// <summary>
    /// How many times the dialog was shown.
    /// </summary>
    public int Shown { get; private set; }

    /// <inheritdoc />
    public Task<Bookmark?> ShowAsync()
    {
        Shown++;
        return Task.FromResult(Answer);
    }
}
