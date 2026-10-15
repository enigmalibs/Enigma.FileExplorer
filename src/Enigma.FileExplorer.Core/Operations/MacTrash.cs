using System;
using System.IO;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// The macOS trash: the entry moves to <c>~/.Trash</c> under a free name. Finder's "Put Back" needs
/// metadata only Finder writes, so items are restored by dragging them out.
/// </summary>
/// <param name="trashDirectory">The trash folder, normally <c>~/.Trash</c>.</param>
public sealed class MacTrash(string trashDirectory) : ITrash
{
    /// <inheritdoc />
    public bool CanTrash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.Exists(path) || Directory.Exists(path);
    }

    /// <inheritdoc />
    public void MoveToTrash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string source = Path.GetFullPath(path);
        string name = Path.GetFileName(source);
        bool isDirectory = Directory.Exists(source);

        if (!isDirectory && !File.Exists(source))
        {
            throw new FileOperationException($"'{name}' does not exist");
        }

        try
        {
            Directory.CreateDirectory(trashDirectory);
            string destination = Path.Combine(trashDirectory, UniqueNameGenerator.FirstFree(trashDirectory, name, isDirectory, UniqueNameGenerator.Exists));

            if (isDirectory)
            {
                Directory.Move(source, destination);
            }
            else
            {
                File.Move(source, destination);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileOperationException($"'{name}' could not be moved to the trash: {exception.Message}", exception);
        }
    }
}
