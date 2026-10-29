using System;
using System.IO;

namespace Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;

/// <summary>
/// The repository the tests run from, for the tests that pin files the build does not ship, such as the
/// packaging scripts.
/// </summary>
internal static class RepositoryRoot
{
    /// <summary>
    /// Finds the repository's root: the first directory above the test's output that holds the solution.
    /// </summary>
    /// <returns>Its full path.</returns>
    public static string Find()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Enigma.FileExplorer.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
    }
}
