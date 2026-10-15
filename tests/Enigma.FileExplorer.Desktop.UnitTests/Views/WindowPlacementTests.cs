using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Enigma.FileExplorer.Core.Settings;
using Enigma.FileExplorer.Desktop.Services;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.ViewModels;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Views;

/// <summary>
/// Restoring the window only where it can be seen, capturing where it is, and the theme switching the
/// running application.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class WindowPlacementTests(HeadlessAvaloniaFixture fixture)
{
    private static readonly PixelRect[] Screens = [new(0, 0, 1920, 1040), new(1920, 0, 2560, 1400)];

    [Theory]
    [InlineData(100, 100, true)]
    [InlineData(3000, 200, true)]
    [InlineData(-50, -10, true)]
    [InlineData(-3000, 100, false)]
    [InlineData(100, 5000, false)]
    public void IsOnScreen_NeedsTheTitleBarOnAScreen(int x, int y, bool expected)
        => Assert.Equal(expected, MainWindow.IsOnScreen(new WindowPlacement { X = x, Y = y, Width = 800, Height = 600 }, Screens));

    [Theory]
    [InlineData(0, 600)]
    [InlineData(800, -1)]
    [InlineData(double.NaN, 600)]
    public void IsOnScreen_RefusesAnImpossibleSize(double width, double height)
        => Assert.False(MainWindow.IsOnScreen(new WindowPlacement { X = 100, Y = 100, Width = width, Height = height }, Screens));

    [Fact]
    public void CapturePlacement_ReportsTheWindowsSize_EvenAfterItClosed()
    {
        WindowPlacement placement = fixture.Run(() =>
        {
            using ServiceProvider provider = CompositionRootTests.BuildProvider();
            MainWindow window = provider.GetRequiredService<MainWindow>();
            App.RegisterHosts(provider, window);
            window.DataContext = provider.GetRequiredService<MainWindowViewModel>();
            window.Width = 900;
            window.Height = 650;
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.Close();
            return window.CapturePlacement();
        });

        Assert.Equal((900, 650, false), (placement.Width, placement.Height, placement.IsMaximized));
    }

    [Fact]
    public void ThemeService_SwitchesTheApplication_AndNeverLeavesItAtDefault()
    {
        (ThemeVariant? light, ThemeVariant? system, int changes) = fixture.Run(() =>
        {
            ThemeService theme = new();
            int count = 0;
            theme.Changed += (_, _) => count++;

            theme.Apply(ThemePreference.Light);
            ThemeVariant? lightVariant = Application.Current?.RequestedThemeVariant;
            theme.Apply(ThemePreference.Light);
            theme.Apply(ThemePreference.System);
            ThemeVariant? systemVariant = Application.Current?.RequestedThemeVariant;

            // Back to the start, for the other tests sharing the application.
            theme.Apply(ThemePreference.Dark);
            return (lightVariant, systemVariant, count);
        });

        Assert.Equal(ThemeVariant.Light, light);
        Assert.True((system == ThemeVariant.Dark) || (system == ThemeVariant.Light));
        Assert.Equal(3, changes);
    }
}
