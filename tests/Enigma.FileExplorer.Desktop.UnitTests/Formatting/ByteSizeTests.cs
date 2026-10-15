using System.Globalization;
using Enigma.FileExplorer.Desktop.Formatting;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Formatting;

public sealed class ByteSizeTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(-5, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10 * 1024, "10 KB")]
    [InlineData(1024 * 1024, "1 MB")]
    [InlineData((long)(2.25 * 1024 * 1024 * 1024), "2.3 GB")]
    [InlineData(500L * 1024 * 1024 * 1024, "500 GB")]
    public void Format_Invariant(long bytes, string expected)
        => Assert.Equal(expected, ByteSize.Format(bytes, CultureInfo.InvariantCulture));

    [Fact]
    public void Format_UsesTheCulturesDecimalSeparator()
        => Assert.Equal("1,5 KB", ByteSize.Format(1536, CultureInfo.GetCultureInfo("fr-CH")));
}
