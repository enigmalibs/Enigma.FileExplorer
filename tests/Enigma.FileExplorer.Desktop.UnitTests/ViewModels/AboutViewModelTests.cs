using Enigma.FileExplorer.Core.Diagnostics;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

/// <summary>
/// What the About dialog says: the running build, and a line only for what there is to show.
/// </summary>
public sealed class AboutViewModelTests
{
    [Fact]
    public void TheViewModel_DescribesTheRunningBuild()
    {
        AboutViewModel about = new(() => { });

        Assert.Equal("Enigma File Explorer", about.DisplayName);
        Assert.Equal(ProductInformation.Version, about.Version);
        Assert.Equal(ProductInformation.BuildSha, about.BuildSha);
        Assert.Equal(ProductInformation.Copyright, about.Copyright);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("9a1b2c3", true)]
    public void TheBuildLine_IsThereOnlyWithARevision(string? sha, bool shown)
        => Assert.Equal(shown, new AboutViewModel("1.0.0", sha, "Copyright", () => { }).HasBuildSha);

    [Theory]
    [InlineData("", false)]
    [InlineData("Copyright © 2026 Josué Clément", true)]
    public void TheCopyrightLine_IsThereOnlyWithANotice(string copyright, bool shown)
        => Assert.Equal(shown, new AboutViewModel("1.0.0", null, copyright, () => { }).HasCopyright);

    [Fact]
    public void CloseCommand_RunsTheCloseCallback()
    {
        int closed = 0;
        AboutViewModel about = new("1.0.0", null, string.Empty, () => closed++);

        about.CloseCommand.Execute(null);

        Assert.Equal(1, closed);
    }
}
