using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.Services;

/// <summary>
/// The clipboard of the window's platform. It knows nothing until <see cref="Attach"/> hands it the window.
/// </summary>
/// <param name="logger">Where platform refusals are recorded.</param>
public sealed class SystemClipboard(ILogger<SystemClipboard> logger) : ISystemClipboard
{
    private TopLevel? _topLevel;

    /// <summary>
    /// Hands over the window whose clipboard and storage provider are used.
    /// </summary>
    /// <param name="topLevel">The main window.</param>
    public void Attach(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);

        _topLevel = topLevel;
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (_topLevel?.Clipboard is not { } clipboard)
        {
            return;
        }

        await GuardAsync(() => clipboard.SetTextAsync(text));
    }

    /// <inheritdoc />
    public async Task SetFilesAsync(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (_topLevel is not { Clipboard: { } clipboard, StorageProvider: { } storage })
        {
            return;
        }

        await GuardAsync(async () =>
        {
            List<IStorageItem> items = [];

            foreach (string path in paths)
            {
                IStorageItem? item = Directory.Exists(path)
                    ? await storage.TryGetFolderFromPathAsync(new Uri(path))
                    : await storage.TryGetFileFromPathAsync(new Uri(path));

                if (item is not null)
                {
                    items.Add(item);
                }
            }

            if (items.Count > 0)
            {
                await clipboard.SetFilesAsync(items);
            }
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetFilesAsync()
    {
        if (_topLevel?.Clipboard is not { } clipboard)
        {
            return [];
        }

        IReadOnlyList<string> paths = [];

        await GuardAsync(async () =>
        {
            IStorageItem[]? items = await clipboard.TryGetFilesAsync();
            paths = items is null ? [] : [.. items.Select(item => item.TryGetLocalPath()).OfType<string>()];
        });

        return paths;
    }

    private async Task GuardAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Clipboard access is a courtesy to other applications: a platform that refuses must not
            // break a copy or a paste inside the explorer.
            logger.LogWarning(exception, "The system clipboard refused the operation");
        }
    }
}
