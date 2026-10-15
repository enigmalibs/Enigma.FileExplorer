using System;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Formatting;

namespace Enigma.FileExplorer.Desktop.ViewModels;

/// <summary>
/// The content of the "already exists" question: both entries side by side, and "apply to all".
/// </summary>
public sealed class ConflictViewModel : ObservableObject
{
    /// <summary>
    /// Describes a conflict.
    /// </summary>
    /// <param name="conflict">The conflict.</param>
    public ConflictViewModel(ConflictInfo conflict)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        Conflict = conflict;
    }

    /// <summary>
    /// The conflict.
    /// </summary>
    public ConflictInfo Conflict { get; }

    /// <summary>
    /// The question.
    /// </summary>
    public string Question
        => $"'{Path.GetFileName(Conflict.TargetPath)}' already exists in '{Path.GetFileName(Path.GetDirectoryName(Conflict.TargetPath))}'.";

    /// <summary>
    /// What is there now.
    /// </summary>
    public string ExistingText => Describe(Conflict.TargetIsDirectory, Conflict.TargetSize, Conflict.TargetModified);

    /// <summary>
    /// What is arriving.
    /// </summary>
    public string IncomingText => Describe(Directory.Exists(Conflict.SourcePath), Conflict.SourceSize, Conflict.SourceModified);

    /// <summary>
    /// Whether the answer holds for every later conflict of this transfer.
    /// </summary>
    public bool ApplyToAll { get; set => SetProperty(ref field, value); }

    private static string Describe(bool isDirectory, long size, DateTime modified)
    {
        string what = isDirectory ? "Folder" : ByteSize.Format(size);
        return $"{what}, modified {modified.ToString("g", CultureInfo.CurrentCulture)}";
    }
}
