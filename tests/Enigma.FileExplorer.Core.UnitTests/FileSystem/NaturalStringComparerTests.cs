using System.Collections.Generic;
using Enigma.FileExplorer.Core.FileSystem;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.FileSystem;

public sealed class NaturalStringComparerTests
{
    private static readonly NaturalStringComparer Comparer = NaturalStringComparer.Instance;

    [Theory]
    [InlineData("file2", "file10")]
    [InlineData("file9.txt", "file10.txt")]
    [InlineData("a", "B")]
    [InlineData("B", "c")]
    [InlineData("1", "a")]
    [InlineData("img1", "img01a")]
    [InlineData("v1.2", "v1.10")]
    [InlineData("report", "report 2")]
    [InlineData("élan", "fable")]
    [InlineData("x99999999999999999999998", "x99999999999999999999999")]
    public void Compare_OrdersTheFirstBeforeTheSecond(string first, string second)
    {
        Assert.True(Comparer.Compare(first, second) < 0, $"expected '{first}' < '{second}'");
        Assert.True(Comparer.Compare(second, first) > 0, $"expected '{second}' > '{first}'");
    }

    [Theory]
    [InlineData("A", "a")]
    [InlineData("01", "1")]
    public void Compare_IsTotalForNamesThatDifferOnlyInCaseOrLeadingZeros(string first, string second)
    {
        int result = Comparer.Compare(first, second);

        Assert.NotEqual(0, result);
        Assert.Equal(-result, Comparer.Compare(second, first));
    }

    [Fact]
    public void Compare_TreatsEqualStringsAsEqual() => Assert.Equal(0, Comparer.Compare("same", "same"));

    [Fact]
    public void Compare_SortsNullFirst()
    {
        Assert.True(Comparer.Compare(null, "a") < 0);
        Assert.True(Comparer.Compare("a", null) > 0);
        Assert.Equal(0, Comparer.Compare(null, null));
    }

    [Fact]
    public void Sort_ProducesTheOrderPeopleExpect()
    {
        List<string> names = ["file10.txt", "File1.txt", "file2.txt", "alpha", "Beta", "file1.txt"];

        names.Sort(Comparer);

        Assert.Equal(["alpha", "Beta", "File1.txt", "file1.txt", "file2.txt", "file10.txt"], names);
    }
}
