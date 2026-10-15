using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.FileExplorer.Desktop.ViewModels;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.ViewModels;

public sealed class PathSegmentTests
{
    private static readonly string Root = Path.GetPathRoot(Path.GetTempPath())!;

    [Fact]
    public void Split_ReturnsRootFirstThenEachDirectory()
    {
        string path = Path.Combine(Root, "Users", "me");

        IReadOnlyList<PathSegment> segments = PathSegment.Split(path);

        Assert.Equal([Root, Path.Combine(Root, "Users"), path], segments.Select(segment => segment.FullPath));
        Assert.Equal(["Users", "me"], segments.Skip(1).Select(segment => segment.Name));
    }

    [Fact]
    public void Split_NamesTheRootWithoutItsTrailingSeparator()
    {
        string expected = Root.Length > 1 ? Root.TrimEnd(Path.DirectorySeparatorChar) : Root;

        Assert.Equal(expected, PathSegment.Split(Root).Single().Name);
    }

    [Fact]
    public void Split_OfAnEmptyPath_IsEmpty() => Assert.Empty(PathSegment.Split(string.Empty));
}
