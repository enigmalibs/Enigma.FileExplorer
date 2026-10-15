using Enigma.FileExplorer.Core.Commands;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Commands;

public sealed class ExCommandParserTests
{
    private readonly ExCommandParser _parser = CommandCatalog.Default.Parser;

    [Theory]
    [InlineData("vsplit", CommandIds.SplitVertical)]
    [InlineData("vs", CommandIds.SplitVertical)]
    [InlineData(":vs", CommandIds.SplitVertical)]
    [InlineData("  :  sp  ", CommandIds.SplitHorizontal)]
    [InlineData("q", CommandIds.Quit)]
    [InlineData("qa", CommandIds.QuitAll)]
    [InlineData("qall", CommandIds.QuitAll)]
    [InlineData("clo", CommandIds.ClosePane)]
    [InlineData("on", CommandIds.OnlyPane)]
    [InlineData("h", CommandIds.Help)]
    [InlineData("del", CommandIds.Trash)]
    [InlineData("delete!", CommandIds.DeletePermanently)]
    public void Parse_AbbreviationsAndBang(string line, string commandId)
    {
        ExParseResult result = _parser.Parse(line);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(commandId, result.Invocation.CommandId);
    }

    [Fact]
    public void Parse_KeepsTheArgument()
    {
        ExParseResult result = _parser.Parse(":cd ~/Documents/My Files");

        Assert.True(result.Succeeded);
        Assert.Equal("~/Documents/My Files", result.Invocation.Argument);
        Assert.False(result.Invocation.Bang);
    }

    [Fact]
    public void Parse_UnquotesAQuotedArgument()
        => Assert.Equal("New \"big\" folder", _parser.Parse("mkdir \"New \\\"big\\\" folder\"").Invocation?.Argument);

    [Fact]
    public void Parse_AnArgumentRightAfterTheName()
        => Assert.Equal("..", _parser.Parse("cd..").Invocation?.Argument);

    [Fact]
    public void Parse_BangWithArgument()
    {
        ExParseResult result = _parser.Parse("sort! size");

        Assert.True(result.Succeeded);
        Assert.True(result.Invocation.Bang);
        Assert.Equal(CommandIds.Sort, result.Invocation.CommandId);
        Assert.Equal("size", result.Invocation.Argument);
    }

    [Fact]
    public void Parse_AnOptionalArgumentMayBeMissing()
    {
        ExParseResult result = _parser.Parse("mkdir");

        Assert.True(result.Succeeded);
        Assert.Null(result.Invocation.Argument);
    }

    [Theory]
    [InlineData("", "No command")]
    [InlineData(":", "No command")]
    [InlineData("frobnicate", "Not a command: frobnicate")]
    [InlineData("123", "Not a command: 123")]
    [InlineData("v", "Not a command: v")]
    [InlineData("vsplitx", "Not a command: vsplitx")]
    [InlineData("help!", "No ! allowed: help")]
    [InlineData("only now", "Trailing characters: now")]
    [InlineData("view", "Argument required: view")]
    public void Parse_Errors(string line, string error)
    {
        ExParseResult result = _parser.Parse(line);

        Assert.False(result.Succeeded);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void CompleteNames_ListsTheCommandsStartingWithThePrefix()
        => Assert.Equal(["set", "sort", "split"], _parser.CompleteNames("s"));

    [Fact]
    public void Find_PrefersAnExactName()
        => Assert.Equal("qall", _parser.Find("qall")?.Name);
}
