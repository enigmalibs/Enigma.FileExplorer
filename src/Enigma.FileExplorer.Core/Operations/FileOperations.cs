using System;
using System.IO;
using System.Security;
using System.Threading;
using Enigma.FileExplorer.Core.FileSystem;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// Create, rename and permanent delete on the local file system.
/// </summary>
public sealed class FileOperations : IFileOperations
{
    /// <inheritdoc />
    public string CreateDirectory(string parent, string name)
    {
        string path = Target(parent, name);

        return Guard(name, () =>
        {
            // Directory.CreateDirectory succeeds silently on an existing directory; the user asked for a new one.
            Directory.CreateDirectory(path);
            return path;
        });
    }

    /// <inheritdoc />
    public string CreateFile(string parent, string name)
    {
        string path = Target(parent, name);

        return Guard(name, () =>
        {
            using FileStream stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            return path;
        });
    }

    /// <inheritdoc />
    public string Rename(string path, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string source = PathUtilities.Normalize(path);
        string name = Path.GetFileName(source);
        string parent = Path.GetDirectoryName(source) ?? throw new FileOperationException("A root cannot be renamed");

        if (NameValidator.Validate(newName) is { } invalid)
        {
            throw new FileOperationException(invalid);
        }

        if (string.Equals(name, newName, StringComparison.Ordinal))
        {
            return source;
        }

        string target = Path.Combine(parent, newName);
        bool caseOnly = string.Equals(name, newName, StringComparison.OrdinalIgnoreCase);

        if (!caseOnly && UniqueNameGenerator.Exists(target))
        {
            throw new FileOperationException($"'{newName}' already exists");
        }

        return Guard(name, () =>
        {
            if (caseOnly && (PathUtilities.Comparison == StringComparison.OrdinalIgnoreCase))
            {
                // A case-insensitive file system sees the target as the source itself; going through a
                // temporary name makes the change of case real.
                string temporary = Path.Combine(parent, $".{newName}.{Guid.NewGuid():N}.renaming");
                Move(source, temporary);
                Move(temporary, target);
            }
            else
            {
                Move(source, target);
            }

            return target;
        });
    }

    /// <inheritdoc />
    public void DeletePermanently(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string target = PathUtilities.Normalize(path);
        string name = Path.GetFileName(target);

        Guard(name, () =>
        {
            Delete(new FileInfo(target), cancellationToken);
            return target;
        });
    }

    private static void Delete(FileSystemInfo entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool isLink = entry.LinkTarget is not null;
        bool isDirectory = Directory.Exists(entry.FullName) && ((File.GetAttributes(entry.FullName) & FileAttributes.Directory) != 0);

        if (!isDirectory)
        {
            if (!File.Exists(entry.FullName) && !isLink)
            {
                throw new FileNotFoundException($"'{entry.Name}' does not exist", entry.FullName);
            }

            ClearReadOnly(entry.FullName);
            File.Delete(entry.FullName);
            return;
        }

        if (isLink)
        {
            // A link or junction to a directory goes; what it points to stays.
            Directory.Delete(entry.FullName, recursive: false);
            return;
        }

        DirectoryInfo directory = new(entry.FullName);

        foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = false }))
        {
            Delete(child, cancellationToken);
        }

        ClearReadOnly(directory.FullName);
        directory.Delete(recursive: false);
    }

    private static void ClearReadOnly(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);

        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }

    private static void Move(string source, string target)
    {
        if (Directory.Exists(source))
        {
            Directory.Move(source, target);
        }
        else
        {
            File.Move(source, target);
        }
    }

    private static string Target(string parent, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parent);

        if (NameValidator.Validate(name) is { } invalid)
        {
            throw new FileOperationException(invalid);
        }

        string path = Path.Combine(PathUtilities.Normalize(parent), name);

        if (UniqueNameGenerator.Exists(path))
        {
            throw new FileOperationException($"'{name}' already exists");
        }

        return path;
    }

    private static string Guard(string name, Func<string> operation)
    {
        try
        {
            return operation();
        }
        catch (FileOperationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new FileOperationException($"Access to '{name}' is denied", exception);
        }
        catch (SecurityException exception)
        {
            throw new FileOperationException($"Access to '{name}' is denied", exception);
        }
        catch (IOException exception) when (exception.HResult == unchecked((int)0x80070050) || exception.HResult == unchecked((int)0x800700B7))
        {
            throw new FileOperationException($"'{name}' already exists", exception);
        }
        catch (IOException exception)
        {
            throw new FileOperationException($"'{name}': {exception.Message}", exception);
        }
    }
}
