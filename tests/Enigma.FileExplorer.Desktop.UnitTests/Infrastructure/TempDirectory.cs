using System;
using System.IO;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A fresh directory under the system temp path, deleted with everything in it on dispose.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>
    /// Creates the directory.
    /// </summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Enigma.FileExplorer.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// The directory's absolute path, without a trailing separator.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The absolute path of a relative path inside the directory.
    /// </summary>
    /// <param name="relative">The relative path, using either separator.</param>
    /// <returns>The absolute path.</returns>
    public string Combine(string relative)
        => System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

    /// <summary>
    /// Creates a file with the given content, creating its directories.
    /// </summary>
    /// <param name="relative">The file's relative path.</param>
    /// <param name="content">The content.</param>
    /// <returns>The file's absolute path.</returns>
    public string CreateFile(string relative, string content = "")
    {
        string path = Combine(relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a directory.
    /// </summary>
    /// <param name="relative">The directory's relative path.</param>
    /// <returns>The directory's absolute path.</returns>
    public string CreateDirectory(string relative)
    {
        string path = Combine(relative);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(Path, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A temp directory left behind is the operating system's to clean; failing the test over it
            // would hide the test's real outcome.
        }
    }
}
