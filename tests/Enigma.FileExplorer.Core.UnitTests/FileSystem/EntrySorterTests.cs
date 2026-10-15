using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.FileExplorer.Core.FileSystem;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.FileSystem;

public sealed class EntrySorterTests
{
    private static readonly DateTime Monday = new(2026, 10, 12, 9, 0, 0, DateTimeKind.Local);

    private static readonly FileSystemEntry[] Entries =
    [
        File("b.txt", size: 300, modified: Monday.AddDays(1)),
        File("a10.png", size: 100, modified: Monday.AddDays(3)),
        Directory("zeta", modified: Monday),
        File("a2.png", size: 100, modified: Monday.AddDays(2)),
        Directory("Alpha", modified: Monday.AddDays(5)),
        File("readme", size: 50, modified: Monday),
    ];

    [Fact]
    public void Sort_ByNameAscending_PutsDirectoriesFirstInNaturalOrder()
        => AssertOrder(new EntrySort(SortKey.Name, SortDirection.Ascending), "Alpha", "zeta", "a2.png", "a10.png", "b.txt", "readme");

    [Fact]
    public void Sort_ByNameDescending_KeepsDirectoriesFirst()
        => AssertOrder(new EntrySort(SortKey.Name, SortDirection.Descending), "zeta", "Alpha", "readme", "b.txt", "a10.png", "a2.png");

    [Fact]
    public void Sort_BySizeAscending_BreaksTiesByName()
        => AssertOrder(new EntrySort(SortKey.Size, SortDirection.Ascending), "Alpha", "zeta", "readme", "a2.png", "a10.png", "b.txt");

    [Fact]
    public void Sort_BySizeDescending_BreaksTiesByNameAscending()
        => AssertOrder(new EntrySort(SortKey.Size, SortDirection.Descending), "Alpha", "zeta", "b.txt", "a2.png", "a10.png", "readme");

    [Fact]
    public void Sort_ByModifiedAscending()
        => AssertOrder(new EntrySort(SortKey.Modified, SortDirection.Ascending), "zeta", "Alpha", "readme", "b.txt", "a2.png", "a10.png");

    [Fact]
    public void Sort_ByModifiedDescending()
        => AssertOrder(new EntrySort(SortKey.Modified, SortDirection.Descending), "Alpha", "zeta", "a10.png", "a2.png", "b.txt", "readme");

    [Fact]
    public void Sort_ByTypeAscending_GroupsByDescription()
        => AssertOrder(new EntrySort(SortKey.Type, SortDirection.Ascending), "Alpha", "zeta", "readme", "a2.png", "a10.png", "b.txt");

    [Fact]
    public void Sort_ByTypeDescending()
        => AssertOrder(new EntrySort(SortKey.Type, SortDirection.Descending), "Alpha", "zeta", "b.txt", "a2.png", "a10.png", "readme");

    [Fact]
    public void Reversed_FlipsTheDirectionOnly()
    {
        EntrySort sort = new(SortKey.Size, SortDirection.Ascending);

        Assert.Equal(new EntrySort(SortKey.Size, SortDirection.Descending), sort.Reversed());
        Assert.Equal(sort, sort.Reversed().Reversed());
    }

    [Fact]
    public void Default_IsByNameAscending() => Assert.Equal(new EntrySort(SortKey.Name, SortDirection.Ascending), EntrySort.Default);

    private static void AssertOrder(EntrySort sort, params string[] expected)
    {
        List<FileSystemEntry> sorted = EntrySorter.Sort(Entries, sort);

        Assert.Equal(expected, sorted.Select(e => e.Name));
    }

    private static FileSystemEntry File(string name, long size, DateTime modified)
        => new(name, Path.Combine("root", name), EntryKind.File, size, modified, IsHidden: false, IsSymbolicLink: false);

    private static FileSystemEntry Directory(string name, DateTime modified)
        => new(name, Path.Combine("root", name), EntryKind.Directory, 0, modified, IsHidden: false, IsSymbolicLink: false);
}
