using System;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Core.Places;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Core.DependencyInjection;

/// <summary>
/// Composition root helpers for the engine library.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the engine's services: file-system reading, operations, search and settings. Every
        /// one is a stateless or thread-safe singleton, so the desktop app and its panes share them.
        /// </summary>
        /// <returns>The same collection, so calls can be chained.</returns>
        public IServiceCollection AddFileExplorerCore()
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IDirectoryReader, DirectoryReader>();
            services.AddSingleton<IKnownFolders, KnownFolders>();
            services.AddSingleton<IVolumeSource, SystemVolumeSource>();
            services.AddSingleton<IPlacesProvider, PlacesProvider>();
            services.AddSingleton<IFileOperations, FileOperations>();
            services.AddSingleton<ITransferEngine, TransferEngine>();
            services.AddSingleton<IFileSearch, FileSearch>();
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton(provider => TrashFactory.Create(provider.GetRequiredService<IKnownFolders>(), provider.GetRequiredService<TimeProvider>()));
            services.AddSingleton<ISettingsStore>(provider => new JsonSettingsStore(AppPaths.SettingsFile, provider.GetRequiredService<ILogger<JsonSettingsStore>>()));

            return services;
        }
    }
}
