using Avalonia.Input;
using Enigma.FileExplorer.Core.Keymaps;
using Enigma.FileExplorer.Desktop.Input;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests.Input;

public sealed class KeyStrokeMapperTests
{
    [Theory]
    [InlineData(Key.J, KeyModifiers.None, "j", "j")]
    [InlineData(Key.G, KeyModifiers.Shift, "G", "G")]
    [InlineData(Key.OemPeriod, KeyModifiers.Shift, ":", ":")]
    [InlineData(Key.D7, KeyModifiers.Shift, "/", "/")]
    [InlineData(Key.OemQuotes, KeyModifiers.Shift, "?", "?")]
    [InlineData(Key.Z, KeyModifiers.None, "y", "y")]
    [InlineData(Key.D1, KeyModifiers.Shift, "+", "+")]
    [InlineData(Key.OemBackslash, KeyModifiers.Shift, ">", ">")]
    public void APrintableKey_IsTheCharacterTheLayoutProduced(Key key, KeyModifiers modifiers, string symbol, string expected)
        => Assert.Equal(KeyNotation.Parse(expected)[0], KeyStrokeMapper.TryMap(key, modifiers, symbol));

    [Theory]
    [InlineData(Key.W, KeyModifiers.Control, "\u0017", "<C-w>")]
    [InlineData(Key.W, KeyModifiers.Control, null, "<C-w>")]
    [InlineData(Key.F, KeyModifiers.Control | KeyModifiers.Shift, null, "<C-S-f>")]
    [InlineData(Key.D6, KeyModifiers.Control | KeyModifiers.Shift, null, "<C-S-6>")]
    [InlineData(Key.Left, KeyModifiers.Alt, null, "<A-Left>")]
    [InlineData(Key.Escape, KeyModifiers.None, "\u001b", "<Esc>")]
    [InlineData(Key.Enter, KeyModifiers.None, "\r", "<CR>")]
    [InlineData(Key.Back, KeyModifiers.None, "\b", "<BS>")]
    [InlineData(Key.Space, KeyModifiers.None, " ", "<Space>")]
    [InlineData(Key.Delete, KeyModifiers.Shift, null, "<S-Del>")]
    [InlineData(Key.F10, KeyModifiers.Shift, null, "<S-F10>")]
    [InlineData(Key.F2, KeyModifiers.None, null, "<F2>")]
    [InlineData(Key.PageDown, KeyModifiers.None, null, "<PageDown>")]
    [InlineData(Key.Apps, KeyModifiers.None, null, "<Apps>")]
    public void ChordsAndSpecialKeys_UseTheVirtualKey(Key key, KeyModifiers modifiers, string? symbol, string expected)
        => Assert.Equal(KeyNotation.Parse(expected)[0], KeyStrokeMapper.TryMap(key, modifiers, symbol));

    [Fact]
    public void AltGr_ProducingACharacter_IsThatCharacter()
        => Assert.Equal(KeyStroke.Char('@'), KeyStrokeMapper.TryMap(Key.D2, KeyModifiers.Control | KeyModifiers.Alt, "@"));

    [Fact]
    public void WithoutASymbol_LettersFallBackOnTheVirtualKey()
    {
        Assert.Equal(KeyStroke.Char('j'), KeyStrokeMapper.TryMap(Key.J, KeyModifiers.None, null));
        Assert.Equal(KeyStroke.Char('G'), KeyStrokeMapper.TryMap(Key.G, KeyModifiers.Shift, null));
    }

    [Theory]
    [InlineData(Key.LeftShift)]
    [InlineData(Key.LeftCtrl)]
    [InlineData(Key.RightAlt)]
    [InlineData(Key.LWin)]
    public void ALoneModifier_IsNotAStroke(Key key) => Assert.Null(KeyStrokeMapper.TryMap(key, KeyModifiers.None, null));
}
