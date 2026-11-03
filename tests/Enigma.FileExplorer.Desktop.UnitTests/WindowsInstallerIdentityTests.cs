using System.IO;
using Enigma.FileExplorer.Core.Diagnostics;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests;

/// <summary>
/// The Windows installer's identity, written once in <c>packaging/windows/buildmsi.ps1</c>, where nothing in
/// the build reads it: the upgrade code, which makes an MSI replace the installed version rather than install
/// beside it, and the product's name, which the installed application, its folder and its shortcuts carry.
/// </summary>
public sealed class WindowsInstallerIdentityTests
{
    /// <summary>
    /// The upgrade code generated for the first MSI of Enigma File Explorer (FEATURE-5447): every later MSI
    /// must keep it, or it installs a second copy instead of upgrading.
    /// </summary>
    private const string UpgradeCode = "6b9404a4-3818-4205-b6c2-bc3befe8c3eb";

    [Fact]
    public void TheInstaller_KeepsTheUpgradeCodeOfTheProductLine()
    {
        Assert.Contains($"$UpgradeCode = '{UpgradeCode}'", Script());
    }

    [Fact]
    public void TheInstaller_IsNamedAfterTheProduct()
    {
        // The MSI's product name, its install folder and its two shortcuts all take this one line.
        Assert.Contains($"$ProductName = '{ProductInformation.DisplayName}'", Script());
    }

    private static string[] Script()
        => File.ReadAllLines(Path.Combine(RepositoryRoot.Find(), "packaging", "windows", "buildmsi.ps1"));
}
