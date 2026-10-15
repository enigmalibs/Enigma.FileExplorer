using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests;

/// <summary>
/// Opens the real main window on the headless platform: the theme merges, the XAML loads and the
/// Enigma hosts accept their registration.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class MainWindowTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void MainWindow_OpensWithTheProductTitle()
    {
        string title = fixture.Run(() =>
        {
            using ServiceProvider provider = CompositionRootTests.BuildProvider();
            MainWindow window = provider.GetRequiredService<MainWindow>();

            App.RegisterHosts(provider, window);
            window.DataContext = provider.GetRequiredService<MainWindowViewModel>();
            window.Show();

            string shown = window.Title ?? string.Empty;
            window.Close();
            return shown;
        });

        Assert.Equal(MainWindowViewModel.ProductName, title);
    }
}
