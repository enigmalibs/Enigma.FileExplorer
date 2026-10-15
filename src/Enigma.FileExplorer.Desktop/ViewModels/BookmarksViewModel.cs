using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Desktop.Services;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The bookmarks dialog: the bookmarked folders, a selection driven like the listing's cursor, and the
/// choice of the folder to open.
/// </summary>
public sealed class BookmarksViewModel : ObservableObject
{
    private readonly Bookmarks _bookmarks;
    private readonly Func<string?> _currentFolder;
    private readonly Action _close;

    /// <summary>
    /// Creates the dialog's ViewModel.
    /// </summary>
    /// <param name="bookmarks">The bookmarks it shows and edits.</param>
    /// <param name="currentFolder">The active pane's folder, which <c>a</c> bookmarks; <see langword="null"/> when it shows no folder.</param>
    /// <param name="close">Closes the dialog.</param>
    public BookmarksViewModel(Bookmarks bookmarks, Func<string?> currentFolder, Action close)
    {
        ArgumentNullException.ThrowIfNull(bookmarks);
        ArgumentNullException.ThrowIfNull(currentFolder);
        ArgumentNullException.ThrowIfNull(close);

        _bookmarks = bookmarks;
        _currentFolder = currentFolder;
        _close = close;
        Items = [.. bookmarks.Items];
        SelectedIndex = Items.Count > 0 ? 0 : -1;

        OpenCommand = new RelayCommand(Open);
        RemoveCommand = new RelayCommand(Remove);
        AddCurrentCommand = new RelayCommand(AddCurrent);
        CloseCommand = new RelayCommand(close);
    }

    /// <summary>
    /// The bookmarks, oldest first.
    /// </summary>
    public IReadOnlyList<Bookmark> Items
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    /// <summary>
    /// Whether there is no bookmark, which the dialog explains.
    /// </summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>
    /// The index of the selected bookmark; -1 when the list is empty.
    /// </summary>
    public int SelectedIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, Items.Count == 0 ? -1 : Math.Clamp(value, 0, Items.Count - 1)))
            {
                OnPropertyChanged(nameof(Selected));
            }
        }
    }

    /// <summary>
    /// The selected bookmark.
    /// </summary>
    public Bookmark? Selected => SelectedIndex >= 0 ? Items[SelectedIndex] : null;

    /// <summary>
    /// The bookmark chosen to open, once the dialog closes on it.
    /// </summary>
    public Bookmark? Chosen { get; private set; }

    /// <summary>
    /// Enter, <c>l</c> or a double-click: opens the selected bookmark in the current pane.
    /// </summary>
    public RelayCommand OpenCommand { get; }

    /// <summary>
    /// <c>d</c> or Del: removes the selected bookmark.
    /// </summary>
    public RelayCommand RemoveCommand { get; }

    /// <summary>
    /// <c>a</c>: bookmarks the current folder.
    /// </summary>
    public RelayCommand AddCurrentCommand { get; }

    /// <summary>
    /// <c>q</c>: closes without opening anything.
    /// </summary>
    public RelayCommand CloseCommand { get; }

    /// <summary>
    /// Moves the selection, stopping at either end.
    /// </summary>
    /// <param name="delta">How far; negative moves up.</param>
    public void Move(int delta) => SelectedIndex = Math.Max(SelectedIndex, 0) + delta;

    /// <summary>
    /// Opens the bookmark the dialog's Open button closed it on, when Enter did not choose one already.
    /// </summary>
    public void ChooseSelected() => Chosen ??= Selected;

    private void Open()
    {
        if (Selected is null)
        {
            return;
        }

        Chosen = Selected;
        _close();
    }

    private void Remove()
    {
        if (Selected is not { } selected)
        {
            return;
        }

        int index = SelectedIndex;
        _bookmarks.Remove(selected);
        Refresh(index);
    }

    private void AddCurrent()
    {
        if (_currentFolder() is not { } folder)
        {
            return;
        }

        _bookmarks.Add(folder);

        // Selected either way: the new bookmark, or the one the folder already had.
        Refresh(FindIndex(folder));
    }

    private int FindIndex(string folder)
    {
        for (int i = 0; i < _bookmarks.Items.Count; i++)
        {
            if (PathUtilities.AreSame(_bookmarks.Items[i].Path, folder))
            {
                return i;
            }
        }

        return SelectedIndex;
    }

    private void Refresh(int selectedIndex)
    {
        Items = [.. _bookmarks.Items];

        // Set after the list, and through a reset, so a selection at an unchanged index still reports.
        SelectedIndex = -1;
        SelectedIndex = selectedIndex;
    }
}
