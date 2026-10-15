using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enigma.FileExplorer.Core.Search;
using Enigma.FileExplorer.Core.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Search;

public sealed class FileSearchTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FileSearch _search = new();

    public FileSearchTests()
    {
        _temp.CreateFile("Report.pdf", "%PDF");
        _temp.CreateFile("notes.md", "first line\nThe Quick fox\nquick again\n");
        _temp.CreateFile("docs/report-2.txt", "nothing to see\nquick\n");
        _temp.CreateFile("docs/deep/deeper/readme.md", "Quick start");
        _temp.CreateFile(".hidden/secret-report.txt", "quick secret");
        _temp.CreateFile(".dotfile", "quick dot");
        _temp.CreateDirectory("reports");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task Name_Substring_IsSmartCase()
    {
        List<SearchHit> lower = await SearchAsync("report", SearchKind.Name);
        List<SearchHit> upper = await SearchAsync("Report", SearchKind.Name);

        Assert.Equal(["Report.pdf", "reports", "report-2.txt"], lower.Select(hit => hit.Entry.Name));
        Assert.Equal(["Report.pdf"], upper.Select(hit => hit.Entry.Name));
    }

    [Fact]
    public async Task Name_Glob_MatchesWholeNames()
    {
        List<SearchHit> markdown = await SearchAsync("*.md", SearchKind.Name);
        List<SearchHit> single = await SearchAsync("?otes.md", SearchKind.Name);
        List<SearchHit> partial = await SearchAsync("*.m", SearchKind.Name);

        Assert.Equal(["notes.md", "readme.md"], markdown.Select(hit => hit.Entry.Name));
        Assert.Equal(["notes.md"], single.Select(hit => hit.Entry.Name));
        Assert.Empty(partial);
    }

    [Fact]
    public async Task Name_NearestFirst_WithTheirFolderRelativeToTheRoot()
    {
        List<SearchHit> hits = await SearchAsync("*.md", SearchKind.Name);

        Assert.Equal(string.Empty, hits[0].RelativeDirectory);
        Assert.Equal(Path.Combine("docs", "deep", "deeper"), hits[1].RelativeDirectory);
    }

    [Fact]
    public async Task Hidden_IsLeftOutUnlessAsked()
    {
        List<SearchHit> without = await SearchAsync("secret", SearchKind.Name);
        List<SearchHit> with = await SearchAsync("secret", SearchKind.Name, includeHidden: true);

        Assert.Empty(without);
        Assert.Single(with);
    }

    [Fact]
    public async Task Content_ReportsEachMatchingLine_SmartCase()
    {
        List<SearchHit> hits = await SearchAsync("quick", SearchKind.Content);

        Assert.Equal(
            [("notes.md", 2), ("notes.md", 3), ("report-2.txt", 2), ("readme.md", 1)],
            hits.Select(hit => (hit.Entry.Name, hit.LineNumber ?? 0)));
        Assert.Equal("The Quick fox", hits[0].LineText);

        List<SearchHit> exact = await SearchAsync("Quick", SearchKind.Content);
        Assert.Equal(["notes.md", "readme.md"], exact.Select(hit => hit.Entry.Name));
    }

    [Fact]
    public async Task Content_SkipsBinaryAndHugeFiles_ButReadsUtf16()
    {
        File.WriteAllBytes(_temp.Combine("binary.dat"), [.. Encoding.ASCII.GetBytes("needle"), 0, 1, 2]);
        File.WriteAllText(_temp.Combine("utf16.txt"), "a needle here", Encoding.Unicode);
        using (FileStream huge = File.Create(_temp.Combine("huge.log")))
        {
            huge.Write(Encoding.ASCII.GetBytes("needle\n"));
            huge.SetLength(FileSearch.MaximumContentSize + 1);
        }

        List<SearchHit> hits = await SearchAsync("needle", SearchKind.Content);

        Assert.Equal(["utf16.txt"], hits.Select(hit => hit.Entry.Name));
    }

    [Fact]
    public async Task Content_CapsTheLinesPerFile()
    {
        File.WriteAllLines(_temp.Combine("many.txt"), Enumerable.Repeat("match", FileSearch.MaximumLinesPerFile + 50));

        List<SearchHit> hits = await SearchAsync("match", SearchKind.Content);

        Assert.Equal(FileSearch.MaximumLinesPerFile, hits.Count(hit => hit.Entry.Name == "many.txt"));
    }

    [Fact]
    public async Task Results_AreCapped()
    {
        foreach (int i in Enumerable.Range(0, 30))
        {
            _temp.CreateFile($"bulk/file{i}.log");
        }

        List<SearchHit> hits = await SearchAsync("*.log", SearchKind.Name, maxResults: 10);

        Assert.Equal(10, hits.Count);
    }

    [Fact]
    public async Task Cancellation_StopsTheSearch()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (SearchHit hit in _search.SearchAsync(new SearchQuery(_temp.Path, "quick", SearchKind.Content), cancellation.Token))
            {
                Assert.Fail($"No hit was expected, got {hit.Entry.Name}");
            }
        });
    }

    [Fact]
    public async Task StoppingEarly_StopsTheWalk()
    {
        int seen = 0;

        await foreach (SearchHit hit in _search.SearchAsync(new SearchQuery(_temp.Path, "*", SearchKind.Name), TestContext.Current.CancellationToken))
        {
            seen++;
            break;
        }

        Assert.Equal(1, seen);
    }

    [Fact]
    public async Task Links_AreNotEntered()
    {
        try
        {
            Directory.CreateSymbolicLink(_temp.Combine("loop"), _temp.Path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Assert.Skip("Creating symbolic links needs a privilege on this machine.");
        }

        List<SearchHit> hits = await SearchAsync("notes.md", SearchKind.Name);

        Assert.Single(hits);
    }

    [Theory]
    [InlineData("  short line  ", 2, "short line")]
    public void Snippet_TrimsShortLines(string line, int index, string expected) => Assert.Equal(expected, FileSearch.Snippet(line, index));

    [Fact]
    public void Snippet_ShortensLongLinesAroundTheMatch()
    {
        string line = new string('a', 500) + "NEEDLE" + new string('b', 500);

        string snippet = FileSearch.Snippet(line, 500);

        Assert.Contains("NEEDLE", snippet, StringComparison.Ordinal);
        Assert.StartsWith("…", snippet, StringComparison.Ordinal);
        Assert.EndsWith("…", snippet, StringComparison.Ordinal);
        Assert.Equal(FileSearch.MaximumLineLength + 2, snippet.Length);
    }

    [Theory]
    [InlineData("*.TXT", "notes.txt", false)]
    [InlineData("*.txt", "NOTES.TXT", true)]
    [InlineData("a?c", "abc", true)]
    [InlineData("a?c", "abbc", false)]
    [InlineData("a.c", "abc", false)]
    [InlineData("a+b", "x a+b y", true)]
    public void NamePattern_Matches(string pattern, string name, bool expected)
        => Assert.Equal(expected, NamePattern.Parse(pattern).IsMatch(name));

    private async Task<List<SearchHit>> SearchAsync(string pattern, SearchKind kind, bool includeHidden = false, int maxResults = SearchQuery.DefaultMaxResults)
    {
        List<SearchHit> hits = [];

        await foreach (SearchHit hit in _search.SearchAsync(new SearchQuery(_temp.Path, pattern, kind, includeHidden, maxResults), TestContext.Current.CancellationToken))
        {
            hits.Add(hit);
        }

        return hits;
    }
}
