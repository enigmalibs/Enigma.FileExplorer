using System;
using System.IO;
using System.Text;

namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// Writes a file so that it is either the old version or the new one, never half of each: the text goes to
/// a temporary file beside it, which then replaces it.
/// </summary>
public static class AtomicFile
{
    /// <summary>
    /// Writes text as UTF-8, creating the directory when needed.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="contents">The text.</param>
    /// <exception cref="IOException">The file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file or its directory is not writable.</exception>
    public static void WriteAllText(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(contents);

        string fullPath = Path.GetFullPath(path);

        if (Path.GetDirectoryName(fullPath) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        string temporary = fullPath + ".tmp";

        try
        {
            using (FileStream stream = new(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(contents);
                stream.Write(bytes);

                // On disk before the rename, so a crash cannot leave the new name on an empty file.
                stream.Flush(flushToDisk: true);
            }

            // A rename within one directory replaces the old file in one step on every platform.
            File.Move(temporary, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The write already failed and is being reported; a leftover temporary file is harmless.
        }
    }
}
