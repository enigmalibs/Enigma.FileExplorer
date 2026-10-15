using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The explorer's options, shared by every pane and set with <c>:set</c>. Hidden files are one switch for the
/// whole explorer, as in ranger: <c>zh</c> in one pane shows them everywhere.
/// </summary>
public sealed class ViewOptions : ObservableObject
{
    /// <summary>
    /// Whether hidden entries are listed.
    /// </summary>
    public bool ShowHidden { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Whether moving to the trash asks first.
    /// </summary>
    public bool ConfirmTrash { get; set => SetProperty(ref field, value); } = true;
}
