using System;
using System.Diagnostics;
using Enigma.FileExplorer.Desktop.Services;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Services;

public sealed class ShellLauncherTests
{
    [Fact]
    public void CreateStartInfo_OnWindows_UsesTheShell()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only.");

        ProcessStartInfo info = ShellLauncher.CreateStartInfo(@"C:\data\report.pdf");

        Assert.True(info.UseShellExecute);
        Assert.Equal(@"C:\data\report.pdf", info.FileName);
    }

    [Fact]
    public void CreateStartInfo_ElsewhereRunsThePlatformOpenerWithThePathAsOneArgument()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Linux and macOS only.");

        ProcessStartInfo info = ShellLauncher.CreateStartInfo("/data/a file; rm -rf ~.txt");

        Assert.False(info.UseShellExecute);
        Assert.Equal(OperatingSystem.IsMacOS() ? "open" : "xdg-open", info.FileName);
        Assert.Equal(["/data/a file; rm -rf ~.txt"], info.ArgumentList);
    }
}
