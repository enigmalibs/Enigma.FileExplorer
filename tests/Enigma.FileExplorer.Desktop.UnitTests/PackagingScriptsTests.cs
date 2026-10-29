using System;
using System.IO;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests;

/// <summary>
/// The application's assembly name is written in places the compiler cannot reach — the publish scripts,
/// which name its project, the Linux installer and uninstaller, which look for the launcher it becomes, and
/// the desktop entry, which matches the window class Avalonia takes from it — and nothing but this test ties
/// them to the assembly the build really produces.
/// </summary>
public sealed class PackagingScriptsTests
{
    private static readonly string AssemblyName = typeof(App).Assembly.GetName().Name ?? string.Empty;

    [Theory]
    [InlineData("windows", "publish.ps1", "win-x64")]
    [InlineData("windows", "publish.bat", "win-x64")]
    [InlineData("linux", "publish.sh", "linux-x64")]
    public void ThePublishScripts_PublishTheProjectOfTheAssembly(string platform, string script, string runtime)
    {
        string text = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "packaging", platform, script));

        // Each script resolves the project from where it sits, with its own platform's separator.
        Assert.True(
            text.Contains($"src/{AssemblyName}", StringComparison.Ordinal)
                || text.Contains($"src\\{AssemblyName}", StringComparison.Ordinal),
            $"{script} does not name src/{AssemblyName}");
        Assert.Contains($"-c Release -r {runtime} -p:PublishReadyToRun=true", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("install.sh")]
    [InlineData("uninstall.sh")]
    public void TheInstallerAndTheUninstaller_LookForTheLauncherTheBuildProduces(string script)
    {
        string[] lines = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), "packaging", "linux", script));

        Assert.Contains($"readonly APP_EXE='{AssemblyName}'", lines);
    }

    [Fact]
    public void TheLauncherEntry_ExpectsTheWindowClassOfTheAssembly()
    {
        string[] entry = File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), "packaging", "linux", "enigma-file-explorer.desktop.in"));

        Assert.Contains($"StartupWMClass={AssemblyName}", entry);
    }
}
