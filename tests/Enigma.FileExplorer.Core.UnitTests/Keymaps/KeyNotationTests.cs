using System;
using System.Collections.Generic;
using Enigma.FileExplorer.Core.Keymaps;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Keymaps;

public sealed class KeyNotationTests
{
    [Fact]
    public void Parse_PlainCharacters()
        => Assert.Equal([KeyStroke.Char('g'), KeyStroke.Char('g')], KeyNotation.Parse("gg"));

    [Fact]
    public void Parse_AChordThenACharacter()
        => Assert.Equal([new KeyStroke("w", StrokeModifiers.Control), KeyStroke.Char('v')], KeyNotation.Parse("<C-w>v"));

    [Theory]
    [InlineData("<Esc>", "Esc", StrokeModifiers.None)]
    [InlineData("<esc>", "Esc", StrokeModifiers.None)]
    [InlineData("<Escape>", "Esc", StrokeModifiers.None)]
    [InlineData("<Enter>", "CR", StrokeModifiers.None)]
    [InlineData("<cr>", "CR", StrokeModifiers.None)]
    [InlineData("<S-Del>", "Del", StrokeModifiers.Shift)]
    [InlineData("<s-delete>", "Del", StrokeModifiers.Shift)]
    [InlineData("<A-Left>", "Left", StrokeModifiers.Alt)]
    [InlineData("<M-Left>", "Left", StrokeModifiers.Alt)]
    [InlineData("<f10>", "F10", StrokeModifiers.None)]
    [InlineData("<S-F10>", "F10", StrokeModifiers.Shift)]
    [InlineData("<Space>", "Space", StrokeModifiers.None)]
    [InlineData("<C-S-6>", "6", StrokeModifiers.Control | StrokeModifiers.Shift)]
    [InlineData("<C-F>", "f", StrokeModifiers.Control)]
    [InlineData("<C-S-f>", "f", StrokeModifiers.Control | StrokeModifiers.Shift)]
    [InlineData("<lt>", "<", StrokeModifiers.None)]
    [InlineData("<Apps>", "Apps", StrokeModifiers.None)]
    public void Parse_ABracketedKey(string notation, string key, StrokeModifiers modifiers)
    {
        KeyStroke stroke = Assert.Single(KeyNotation.Parse(notation));

        Assert.Equal(key, stroke.Key);
        Assert.Equal(modifiers, stroke.Modifiers);
    }

    [Fact]
    public void Parse_AnUnknownBracketIsLiteral()
        => Assert.Equal(["<", "n", "o", "p", "e", ">"], Keys(KeyNotation.Parse("<nope>")));

    [Fact]
    public void Parse_ALoneLessThanIsLiteral() => Assert.Equal(["<", "x"], Keys(KeyNotation.Parse("<x")));

    [Fact]
    public void Parse_AChordWhoseKeyIsTheClosingBracket()
        => Assert.Equal(new KeyStroke(">", StrokeModifiers.Control), Assert.Single(KeyNotation.Parse("<C->>")));

    [Fact]
    public void Parse_GreaterThanAfterAChord() => Assert.Equal(["w", ">"], Keys(KeyNotation.Parse("<C-w>>")));

    [Fact]
    public void Parse_RejectsEmpty() => Assert.Throws<FormatException>(() => KeyNotation.Parse(string.Empty));

    [Theory]
    [InlineData("gg")]
    [InlineData("<C-w>v")]
    [InlineData("<S-Del>")]
    [InlineData("<C-S-6>")]
    [InlineData("<C-w><lt>")]
    [InlineData("<lt>")]
    [InlineData("G")]
    [InlineData("<Space>")]
    [InlineData("<A-Left>")]
    public void Format_RoundTrips(string notation) => Assert.Equal(notation, KeyNotation.Format(KeyNotation.Parse(notation)));

    [Theory]
    [InlineData("gg", "gg")]
    [InlineData("dD", "dD")]
    [InlineData("<C-w>v", "Ctrl+W v")]
    [InlineData("<S-Del>", "Shift+Del")]
    [InlineData("<C-S-f>", "Ctrl+Shift+F")]
    [InlineData("<CR>", "Enter")]
    [InlineData("<A-Left>", "Alt+←")]
    [InlineData("<F2>", "F2")]
    [InlineData("?", "?")]
    public void Describe_ReadsAsPeopleWriteIt(string notation, string expected)
        => Assert.Equal(expected, KeyNotation.Describe(KeyNotation.Parse(notation)));

    [Fact]
    public void KeyStroke_APlainCharacterIgnoresShift()
        => Assert.Equal(KeyStroke.Char('G'), new KeyStroke("G", StrokeModifiers.Shift));

    [Fact]
    public void KeyStroke_RejectsAnUnknownName() => Assert.Throws<ArgumentException>(() => new KeyStroke("Nope"));

    [Fact]
    public void KeyStroke_DigitsAreDigits()
    {
        Assert.True(KeyStroke.Char('5').IsDigit);
        Assert.False(new KeyStroke("5", StrokeModifiers.Control).IsDigit);
        Assert.False(KeyStroke.Char('x').IsDigit);
    }

    private static List<string> Keys(IReadOnlyList<KeyStroke> strokes)
    {
        List<string> keys = [];

        foreach (KeyStroke stroke in strokes)
        {
            keys.Add(stroke.Key);
        }

        return keys;
    }
}
