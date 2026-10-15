using System.Collections.Generic;
using System.IO;
using Enigma.FileExplorer.Core.Operations;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Operations;

public sealed class NameRulesTests
{
    [Theory]
    [InlineData("report.pdf")]
    [InlineData(".gitignore")]
    [InlineData("name with spaces")]
    [InlineData("élève — notes")]
    [InlineData("console.log")]
    public void Validate_AcceptsOrdinaryNames(string name)
    {
        Assert.Null(NameValidator.Validate(name, NamePlatform.Windows));
        Assert.Null(NameValidator.Validate(name, NamePlatform.Unix));
    }

    [Theory]
    [InlineData("", "A name cannot be empty")]
    [InlineData("   ", "A name cannot be empty")]
    [InlineData(".", "'.' and '..' are not valid names")]
    [InlineData("..", "'.' and '..' are not valid names")]
    [InlineData("a/b", "A name cannot contain '/' or '\\'")]
    [InlineData("a\\b", "A name cannot contain '/' or '\\'")]
    [InlineData("what?", "A name cannot contain any of <>:\"|?*")]
    [InlineData("a:b", "A name cannot contain any of <>:\"|?*")]
    [InlineData("tab\there", "A name cannot contain control characters")]
    [InlineData("trailing.", "A name cannot end with a dot or a space")]
    [InlineData("trailing ", "A name cannot end with a dot or a space")]
    [InlineData("CON", "'CON' is reserved by Windows")]
    [InlineData("con.txt", "'con' is reserved by Windows")]
    [InlineData("LPT1.log", "'LPT1' is reserved by Windows")]
    public void Validate_Windows_Rejects(string name, string message)
        => Assert.Equal(message, NameValidator.Validate(name, NamePlatform.Windows));

    [Theory]
    [InlineData("a\\b")]
    [InlineData("what?")]
    [InlineData("CON")]
    [InlineData("trailing.")]
    public void Validate_Unix_AllowsWhatOnlyWindowsForbids(string name)
        => Assert.Null(NameValidator.Validate(name, NamePlatform.Unix));

    [Theory]
    [InlineData("a/b", "A name cannot contain '/'")]
    [InlineData("nul\0char", "A name cannot contain a NUL character")]
    [InlineData("..", "'.' and '..' are not valid names")]
    public void Validate_Unix_Rejects(string name, string message)
        => Assert.Equal(message, NameValidator.Validate(name, NamePlatform.Unix));

    [Fact]
    public void Validate_RejectsOverlongNames()
        => Assert.Equal("A name cannot be longer than 255 characters", NameValidator.Validate(new string('a', 256), NamePlatform.Unix));

    [Theory]
    [InlineData("report.pdf", false, "report (2).pdf")]
    [InlineData("report (2).pdf", false, "report (3).pdf")]
    [InlineData("photos", true, "photos (2)")]
    [InlineData("v1.2", true, "v1.2 (2)")]
    [InlineData(".bashrc", false, ".bashrc (2)")]
    [InlineData("archive.tar.gz", false, "archive.tar (2).gz")]
    public void FirstFree_NumbersTakenNames(string name, bool isDirectory, string expected)
    {
        HashSet<string> taken = [Path.Combine("dir", name)];

        Assert.Equal(expected, UniqueNameGenerator.FirstFree("dir", name, isDirectory, taken.Contains));
    }

    [Fact]
    public void FirstFree_SkipsEveryTakenNumber()
    {
        HashSet<string> taken = [Path.Combine("dir", "a.txt"), Path.Combine("dir", "a (2).txt"), Path.Combine("dir", "a (3).txt")];

        Assert.Equal("a (4).txt", UniqueNameGenerator.FirstFree("dir", "a.txt", false, taken.Contains));
    }

    [Fact]
    public void FirstFree_KeepsAFreeName() => Assert.Equal("free.txt", UniqueNameGenerator.FirstFree("dir", "free.txt", false, _ => false));
}
