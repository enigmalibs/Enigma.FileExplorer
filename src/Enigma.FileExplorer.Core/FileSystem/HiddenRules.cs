using System;
using System.IO;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Decides which entries are hidden.
/// </summary>
/// <remarks>
/// An entry is hidden when its name starts with a dot (the Unix convention, which Windows users meet in
/// every cloned repository) or when it carries the <see cref="FileAttributes.Hidden"/> attribute (the
/// Windows convention). Either one is enough.
/// </remarks>
public static class HiddenRules
{
    /// <summary>
    /// Whether an entry with this name and these attributes is hidden.
    /// </summary>
    /// <param name="name">The entry's name.</param>
    /// <param name="attributes">The entry's attributes.</param>
    /// <returns><see langword="true"/> when the entry is hidden.</returns>
    public static bool IsHidden(string name, FileAttributes attributes)
    {
        ArgumentNullException.ThrowIfNull(name);

        return name.StartsWith('.') || ((attributes & FileAttributes.Hidden) != 0);
    }
}
