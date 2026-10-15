using System;
using System.Collections.Generic;
using System.IO;
using Enigma.FileExplorer.Core.Commands;
using Enigma.FileExplorer.Core.Search;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Commands;

public sealed class CommandLineToolsTests
{
    private static readonly string Root = Path.GetPathRoot(Path.GetTempPath())!;
    private static readonly string Current = Path.Combine(Root, "work");
    private static readonly string Home = Path.Combine(Root, "home", "me");

    private static readonly Dictionary<string, string[]> Tree = new(StringComparer.OrdinalIgnoreCase)
    {
        [Current] = ["src", "Scripts", "docs", ".git"],
        [Path.Combine(Current, "src")] = ["App", "Core"],
        [Home] = ["Documents", "Downloads", "Music"],
    };

    private readonly CommandLineCompleter _completer = new(CommandCatalog.Default.Parser);

    [Fact]
    public void Complete_CommandNames() => Assert.Equal(["set", "sort", "split"], Complete("s"));

    [Fact]
    public void Complete_FixedValues() => Assert.Equal(["view details"], Complete("view d"));

    [Fact]
    public void Complete_AllValuesWhenNothingIsTyped() => Assert.Equal(["theme dark", "theme light", "theme system"], Complete("theme "));

    [Fact]
    public void Complete_PathsRelativeToTheCurrentDirectory_CaseInsensitively_HiddenLeftOut()
    {
        char separator = Path.DirectorySeparatorChar;

        Assert.Equal([$"cd Scripts{separator}", $"cd src{separator}"], Complete("cd s"));
        Assert.Equal([$"cd docs{separator}", $"cd Scripts{separator}", $"cd src{separator}"], Complete("cd "));
    }

    [Fact]
    public void Complete_HiddenDirectoriesWhenAskedFor()
        => Assert.Equal([$"cd .git{Path.DirectorySeparatorChar}"], Complete("cd .g"));

    [Fact]
    public void Complete_InsideATypedDirectory_KeepsItsSeparator()
        => Assert.Equal(["vsplit src/App/", "vsplit src/Core/"], Complete("vsplit src/"));

    [Fact]
    public void Complete_UnderHome() => Assert.Equal(["cd ~/Documents/", "cd ~/Downloads/"], Complete("cd ~/Do"));

    [Fact]
    public void Complete_ATildeAloneBecomesHome() => Assert.Equal(["cd ~/"], Complete("cd ~"));

    [Fact]
    public void Complete_NothingForFreeTextOrUnknownCommands()
    {
        Assert.Empty(Complete("mkdir x"));
        Assert.Empty(Complete("frob x"));
    }

    [Fact]
    public void History_SkipsDuplicatesAndKeepsTheNewest()
    {
        CommandHistory history = new();
        history.Add("cd a");
        history.Add("vs");
        history.Add("cd a");
        history.Add("   ");

        Assert.Equal(["vs", "cd a"], history.Entries);
    }

    [Fact]
    public void History_BrowsesOlderAndNewer_FilteredByWhatWasTyped()
    {
        CommandHistory history = new();
        history.Add("cd one");
        history.Add("vs");
        history.Add("cd two");

        Assert.Equal("cd two", history.Older("cd"));
        Assert.Equal("cd one", history.Older("cd two"));
        Assert.Null(history.Older("cd one"));
        Assert.Equal("cd two", history.Newer());
        Assert.Equal("cd", history.Newer());
        Assert.Null(history.Newer());
    }

    [Fact]
    public void History_IsCapped()
    {
        CommandHistory history = new(capacity: 2);
        history.Add("a");
        history.Add("b");
        history.Add("c");

        Assert.Equal(["b", "c"], history.Entries);
    }

    [Theory]
    [InlineData("Report.PDF", "report", true)]
    [InlineData("report.pdf", "Report", false)]
    [InlineData("Report.pdf", "Report", true)]
    [InlineData("notes.txt", "xyz", false)]
    public void SmartCase_Contains(string text, string pattern, bool expected) => Assert.Equal(expected, SmartCase.Contains(text, pattern));

    private IReadOnlyList<string> Complete(string line)
        => _completer.Complete(line, Current, Home, path => Tree.TryGetValue(path, out string[]? children) ? children : []);
}
