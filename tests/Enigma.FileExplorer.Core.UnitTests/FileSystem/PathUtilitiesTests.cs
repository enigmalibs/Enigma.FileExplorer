using System;
using System.Collections.Generic;
using System.IO;
using Enigma.FileExplorer.Core.FileSystem;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.FileSystem;

public sealed class PathUtilitiesTests
{
    private static readonly string Root = Path.GetPathRoot(Path.GetTempPath())!;
    private static readonly string Base = Path.Combine(Root, "work", "project");
    private static readonly string Home = Path.Combine(Root, "home", "me");

    [Fact]
    public void Normalize_RemovesATrailingSeparator()
        => Assert.Equal(Base, PathUtilities.Normalize(Base + Path.DirectorySeparatorChar));

    [Fact]
    public void Normalize_KeepsARoot() => Assert.Equal(Root, PathUtilities.Normalize(Root));

    [Fact]
    public void Normalize_ResolvesDotDot()
        => Assert.Equal(Path.Combine(Root, "work"), PathUtilities.Normalize(Path.Combine(Base, "..")));

    [Fact]
    public void Resolve_Tilde_IsHome() => Assert.Equal(Home, PathUtilities.Resolve("~", Base, Home));

    [Fact]
    public void Resolve_TildeSlash_IsUnderHome()
        => Assert.Equal(Path.Combine(Home, "Documents"), PathUtilities.Resolve("~/Documents", Base, Home));

    [Fact]
    public void Resolve_Relative_IsUnderTheBase()
        => Assert.Equal(Path.Combine(Base, "src", "app"), PathUtilities.Resolve("src/app", Base, Home));

    [Fact]
    public void Resolve_DotDot_IsTheParent()
        => Assert.Equal(Path.Combine(Root, "work"), PathUtilities.Resolve("..", Base, Home));

    [Fact]
    public void Resolve_Absolute_IsKept()
        => Assert.Equal(Path.Combine(Root, "etc"), PathUtilities.Resolve(Path.Combine(Root, "etc") + Path.DirectorySeparatorChar, Base, Home));

    [Fact]
    public void Resolve_Blank_IsTheBase() => Assert.Equal(Base, PathUtilities.Resolve("   ", Base, Home));

    [Fact]
    public void Resolve_TrimsSurroundingWhitespace()
        => Assert.Equal(Path.Combine(Base, "src"), PathUtilities.Resolve("  src  ", Base, Home));

    [Fact]
    public void Resolve_BareDrive_IsTheDriveRoot()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Drive letters are a Windows concept.");

        Assert.Equal(@"D:\", PathUtilities.Resolve("d:", Base, Home), ignoreCase: true);
    }

    [Fact]
    public void GetParent_OfARoot_IsNull() => Assert.Null(PathUtilities.GetParent(Root));

    [Fact]
    public void GetParent_OfADirectory()
        => Assert.Equal(Path.Combine(Root, "work"), PathUtilities.GetParent(Base));

    [Fact]
    public void GetParent_OfAChildOfTheRoot_IsTheRoot()
        => Assert.Equal(Root, PathUtilities.GetParent(Path.Combine(Root, "work")));

    [Fact]
    public void GetVolumeRoot_IsThePathRoot() => Assert.Equal(Root, PathUtilities.GetVolumeRoot(Base));

    [Fact]
    public void FindNearestExisting_WalksUp()
    {
        HashSet<string> existing = [Path.Combine(Root, "work")];

        Assert.Equal(Path.Combine(Root, "work"), PathUtilities.FindNearestExisting(Path.Combine(Base, "gone", "deeper"), existing.Contains));
    }

    [Fact]
    public void FindNearestExisting_ReturnsThePathItselfWhenItExists()
    {
        using TempDirectory temp = new();

        Assert.Equal(temp.Path, PathUtilities.FindNearestExisting(temp.Path, Directory.Exists));
    }

    [Fact]
    public void FindNearestExisting_ReturnsNullWhenNothingExists()
        => Assert.Null(PathUtilities.FindNearestExisting(Base, _ => false));

    [Fact]
    public void IsSameOrDescendant_RecognisesTheSamePathAndChildren()
    {
        Assert.True(PathUtilities.IsSameOrDescendant(Base, Base));
        Assert.True(PathUtilities.IsSameOrDescendant(Path.Combine(Base, "a", "b"), Base));
        Assert.True(PathUtilities.IsSameOrDescendant(Base, Root));
    }

    [Fact]
    public void IsSameOrDescendant_RejectsSiblingsSharingAPrefix()
    {
        Assert.False(PathUtilities.IsSameOrDescendant(Base + "-copy", Base));
        Assert.False(PathUtilities.IsSameOrDescendant(Path.Combine(Root, "work"), Base));
    }

    [Fact]
    public void AreSame_IgnoresATrailingSeparator()
        => Assert.True(PathUtilities.AreSame(Base, Base + Path.DirectorySeparatorChar));

    [Fact]
    public void AreSame_FollowsThePlatformsCaseRule()
        => Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), PathUtilities.AreSame(Base, Base.ToUpperInvariant()));
}
