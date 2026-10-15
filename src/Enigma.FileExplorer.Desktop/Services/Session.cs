using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.ViewModels;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// What the explorer remembers between runs: loads the settings at start and applies the theme, the options
/// (hidden files, trash confirmation) and the bookmarks, hands the saved pane view to the start, and saves
/// everything at exit — and whenever the theme, an option or the bookmarks change, so a crash does not lose
/// them. Where the panes were is not
/// remembered: the explorer always starts with a single pane.
/// </summary>
/// <param name="store">Where the settings live.</param>
/// <param name="workspace">The panes; the active one's view and sort are captured at each save.</param>
/// <param name="options">The options: hidden files and the trash confirmation.</param>
/// <param name="theme">The colour theme.</param>
/// <param name="bookmarks">The bookmarked folders.</param>
public sealed class Session(ISettingsStore store, WorkspaceViewModel workspace, ViewOptions options, IThemeService theme, Bookmarks bookmarks)
{
    private Func<WindowPlacement?>? _window;
    private AppSettings? _settings;
    private bool _listening;

    /// <summary>
    /// The settings as of the last save, read from the store the first time they are needed. Startup needs
    /// some of them before <see cref="Load"/> applies the rest (the leader key builds the keymap), and both
    /// share this one read.
    /// </summary>
    public AppSettings Settings
    {
        get => _settings ??= store.Load();
        private set => _settings = value;
    }

    /// <summary>
    /// The directory a command line asks for: its first argument, when that names an existing directory.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="directoryExists">Whether a directory exists.</param>
    /// <returns>The full path, or <see langword="null"/>.</returns>
    public static string? DirectoryArgument(IReadOnlyList<string> args, Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(directoryExists);

        if (args is not [{ Length: > 0 } first, ..])
        {
            return null;
        }

        try
        {
            // Relative to where the explorer was started from, so "Enigma.FileExplorer.Desktop ." opens there.
            string path = Path.GetFullPath(first);
            return directoryExists(path) ? path : null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the settings and applies the theme and the hidden-files switch.
    /// </summary>
    /// <returns>The settings.</returns>
    public AppSettings Load()
    {
        AppSettings settings = Settings;
        options.ShowHidden = settings.ShowHidden;
        options.ConfirmTrash = settings.ConfirmTrash;
        bookmarks.Load(settings.Bookmarks.Select(saved => new Bookmark(saved.Name, saved.Path)));
        theme.Apply(settings.Theme);

        // Listening only now: applying what was loaded is not a change worth saving.
        if (!_listening)
        {
            _listening = true;
            theme.Changed += (_, _) => Save();
            options.PropertyChanged += OnOptionsChanged;
            bookmarks.Changed += (_, _) => Save();
        }

        return settings;
    }

    /// <summary>
    /// Tells the session how to read where the main window is, for each save.
    /// </summary>
    /// <param name="capture">Reads the window's placement.</param>
    public void TrackWindow(Func<WindowPlacement?> capture) => _window = capture;

    /// <summary>
    /// Saves the theme, the hidden-files switch, the window and the active pane's view.
    /// </summary>
    public void Save()
    {
        Settings = Settings with
        {
            Theme = theme.Preference,
            ShowHidden = options.ShowHidden,
            ConfirmTrash = options.ConfirmTrash,
            Bookmarks = [.. bookmarks.Items.Select(bookmark => new BookmarkSettings { Name = bookmark.Name, Path = bookmark.Path })],
            Window = _window?.Invoke() ?? Settings.Window,
            Pane = workspace.CaptureView(),
        };

        store.Save(Settings);
    }

    private void OnOptionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewOptions.ShowHidden) or nameof(ViewOptions.ConfirmTrash))
        {
            Save();
        }
    }
}
