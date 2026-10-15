using System.Collections.Generic;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// One row of tiles in an icon view. Icon views list rows rather than tiles so the list can stay
/// virtualized: only the rows on screen are built, whatever the size of the directory.
/// </summary>
/// <param name="Items">The tiles of the row, left to right.</param>
public sealed record EntryRowViewModel(IReadOnlyList<EntryViewModel> Items);
