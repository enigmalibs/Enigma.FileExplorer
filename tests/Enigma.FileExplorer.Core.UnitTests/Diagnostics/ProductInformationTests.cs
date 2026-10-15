using System;
using Enigma.FileExplorer.Core.Diagnostics;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Diagnostics;

/// <summary>
/// What the running build says about itself, as <c>Directory.Build.props</c> stamps it.
/// </summary>
public sealed class ProductInformationTests
{
    [Fact]
    public void Name_IsTheTechnicalName()
        => Assert.Equal("Enigma.FileExplorer", ProductInformation.Name);

    [Fact]
    public void DisplayName_IsTheProductInWords()
        => Assert.Equal("Enigma File Explorer", ProductInformation.DisplayName);

    [Fact]
    public void Version_IsTheBuildsVersion_WithoutTheRevision()
    {
        Assert.False(string.IsNullOrWhiteSpace(ProductInformation.Version));
        Assert.DoesNotContain("+", ProductInformation.Version, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSha_IsShortWhenThereIsOne()
        => Assert.True(ProductInformation.BuildSha is null || ProductInformation.BuildSha.Length <= 7, ProductInformation.BuildSha);

    [Fact]
    public void Copyright_IsTheOneTheBuildStamps()
        => Assert.Contains("Josué Clément", ProductInformation.Copyright, StringComparison.Ordinal);
}
