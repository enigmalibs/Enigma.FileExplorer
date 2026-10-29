using System;
using Enigma.Avalonia.Desktop.Services;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Operations;
using Enigma.FileExplorer.Desktop.Commands;
using Enigma.FileExplorer.Desktop.Input;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Enigma.FileExplorer.Desktop.DependencyInjection;

/// <summary>
/// Composition root helpers for the desktop application.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the six Enigma.Avalonia.Desktop services. The library ships no registration
        /// extension of its own, so this is it.
        /// </summary>
        /// <returns>The same collection, so calls can be chained.</returns>
        public IServiceCollection AddEnigmaDesktopServices()
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();
            services.AddSingleton<IOverlayService, OverlayService>();
            services.AddSingleton<IInfoBarService, InfoBarService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IFolderDialogService, FolderDialogService>();

            return services;
        }

        /// <summary>
        /// Registers the window, the ViewModels and the application's own services.
        /// </summary>
        /// <returns>The same collection, so calls can be chained.</returns>
        public IServiceCollection AddFileExplorerApp()
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IShellLauncher, ShellLauncher>();
            services.AddSingleton<IStatusMessages, StatusMessages>();
            services.AddSingleton<IDirectoryWatcherFactory, DirectoryWatcherFactory>();
            services.AddSingleton<IPaneFactory, PaneFactory>();
            services.AddSingleton<PlacesViewModel>();
            services.AddSingleton<ViewOptions>();

            // Copy and paste: the explorer's clipboard over the system's, and a queue running one transfer at a time.
            services.AddSingleton<SystemClipboard>();
            services.AddSingleton<ISystemClipboard>(provider => provider.GetRequiredService<SystemClipboard>());
            services.AddSingleton<IFileClipboard, FileClipboard>();
            services.AddSingleton<IConflictResolver, ConflictDialogService>();
            services.AddSingleton<ITransferErrorReport, TransferErrorReport>();
            services.AddSingleton<ITransferQueue, TransferQueue>();
            services.AddSingleton<WorkspaceViewModel>();

            // The command line is also the prompt every command asks its questions through.
            services.AddSingleton<CommandLineViewModel>();
            services.AddSingleton<IPromptService>(provider => provider.GetRequiredService<CommandLineViewModel>());
            services.AddSingleton<IHelpDialogService, HelpDialogService>();
            services.AddSingleton<IAboutDialogService, AboutDialogService>();

            // Commands: every module registered here hands its handlers to the dispatcher.
            services.AddSingleton<ICommandModule, NavigationCommands>();
            services.AddSingleton<ICommandModule, CommandLineCommands>();
            services.AddSingleton<ICommandModule, WindowCommands>();
            services.AddSingleton<ICommandModule, ViewCommands>();
            services.AddSingleton<ICommandModule, FileCommands>();
            services.AddSingleton<ICommandModule, ClipboardCommands>();
            services.AddSingleton<ICommandModule, SearchCommands>();
            services.AddSingleton<ICommandModule, MenuCommands>();
            services.AddSingleton<ICommandModule, ThemeCommands>();
            services.AddSingleton<ICommandModule, BookmarkCommands>();
            services.AddSingleton<Bookmarks>();
            services.AddSingleton<IBookmarksDialogService, BookmarksDialogService>();
            services.AddSingleton<IConfirmationService, ConfirmationService>();
            services.AddSingleton<IAppShutdown, AppShutdown>();
            services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
            services.AddSingleton<KeyboardController>();
            services.AddSingleton<ContextMenus>();
            services.AddSingleton<FileDrops>();

            // The safety net. One instance under both names: the composition root hands the concrete one the
            // window's dialog host, and everything else reports through the interface.
            services.TryAddSingleton(TimeProvider.System);
            services.AddSingleton<UnhandledErrorReporter>();
            services.AddSingleton<IUnhandledErrorReporter>(provider => provider.GetRequiredService<UnhandledErrorReporter>());

            // The session: the theme, and what is saved at exit and restored at start. The keymap is built from
            // it too, with the leader key the settings choose.
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<Session>();
            services.AddSingleton(provider => CommandCatalogFactory.Create(
                provider.GetRequiredService<Session>().Settings,
                provider.GetRequiredService<IStatusMessages>(),
                provider.GetRequiredService<ILogger<CommandCatalog>>()));

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            return services;
        }
    }
}
