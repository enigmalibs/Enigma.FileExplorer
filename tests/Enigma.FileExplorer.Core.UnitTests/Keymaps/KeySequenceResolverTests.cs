using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.FileExplorer.Core.Keymaps;
using Xunit;

namespace Enigma.FileExplorer.Core.UnitTests.Keymaps;

public sealed class KeySequenceResolverTests
{
    private readonly KeySequenceResolver _resolver = new(new Keymap(
    [
        KeyBinding.Parse("j", "down"),
        KeyBinding.Parse("gg", "first"),
        KeyBinding.Parse("g1", "place", "1"),
        KeyBinding.Parse("<C-w>v", "vsplit"),
        KeyBinding.Parse("<Esc>", "escape"),
        KeyBinding.Parse("0", "zero"),
    ]));

    [Fact]
    public void ASingleKeyBinding_MatchesAtOnce()
    {
        KeyResolution resolution = Feed("j");

        Assert.Equal(KeyResolutionKind.Matched, resolution.Kind);
        Assert.Equal("down", resolution.Binding?.CommandId);
        Assert.Null(resolution.Count);
        Assert.False(_resolver.IsPending);
    }

    [Fact]
    public void ASequence_IsPendingUntilComplete()
    {
        Assert.Equal(KeyResolutionKind.Pending, Feed("g").Kind);
        Assert.True(_resolver.IsPending);
        Assert.Equal("g", _resolver.PendingText);

        KeyResolution resolution = Feed("g");

        Assert.Equal(KeyResolutionKind.Matched, resolution.Kind);
        Assert.Equal("first", resolution.Binding?.CommandId);
    }

    [Fact]
    public void AChordSequence_Matches()
    {
        Assert.Equal(KeyResolutionKind.Pending, Feed("<C-w>").Kind);
        Assert.Equal("<C-w>", _resolver.PendingText);

        Assert.Equal("vsplit", Feed("v").Binding?.CommandId);
    }

    [Fact]
    public void ABindingArgument_TravelsWithTheMatch()
    {
        Feed("g");

        KeyResolution resolution = Feed("1");

        Assert.Equal("place", resolution.Binding?.CommandId);
        Assert.Equal("1", resolution.Binding?.Argument);
    }

    [Fact]
    public void ACount_PrecedesTheSequence()
    {
        Assert.Equal(KeyResolutionKind.Pending, Feed("1").Kind);
        Assert.Equal(KeyResolutionKind.Pending, Feed("2").Kind);
        Assert.Equal("12", _resolver.PendingText);

        KeyResolution resolution = Feed("j");

        Assert.Equal(12, resolution.Count);
        Assert.Equal(string.Empty, _resolver.PendingText);
    }

    [Fact]
    public void ACount_ExtendsWithZero()
    {
        Feed("1");
        Feed("0");

        Assert.Equal(10, Feed("j").Count);
    }

    [Fact]
    public void AZeroWithoutACount_IsAKeyOfItsOwn() => Assert.Equal("zero", Feed("0").Binding?.CommandId);

    [Fact]
    public void ACount_IsCapped()
    {
        foreach (char digit in "9999999")
        {
            _resolver.Feed(KeyStroke.Char(digit));
        }

        Assert.Equal(KeySequenceResolver.MaximumCount, Feed("j").Count);
    }

    [Fact]
    public void EscapeWhilePending_Cancels()
    {
        Feed("5");
        Feed("g");

        Assert.Equal(KeyResolutionKind.Cancelled, Feed("<Esc>").Kind);
        Assert.False(_resolver.IsPending);
    }

    [Fact]
    public void EscapeWhenIdle_RunsItsBinding() => Assert.Equal("escape", Feed("<Esc>").Binding?.CommandId);

    [Fact]
    public void AWrongContinuation_DiscardsTheSequence()
    {
        Feed("g");

        KeyResolution resolution = Feed("x");

        Assert.Equal(KeyResolutionKind.Discarded, resolution.Kind);
        Assert.True(resolution.IsConsumed);
        Assert.False(_resolver.IsPending);
        Assert.Equal("down", Feed("j").Binding?.CommandId);
    }

    [Fact]
    public void AnUnboundKey_IsNotConsumed()
    {
        KeyResolution resolution = Feed("x");

        Assert.Equal(KeyResolutionKind.Unmatched, resolution.Kind);
        Assert.False(resolution.IsConsumed);
    }

    [Fact]
    public void PendingChanged_FollowsThePendingText()
    {
        List<string> seen = [];
        _resolver.PendingChanged += (_, _) => seen.Add(_resolver.PendingText);

        Feed("3");
        Feed("g");
        Feed("g");

        Assert.Equal(["3", "3g", string.Empty], seen);
    }

    [Fact]
    public void Keymap_RejectsAPrefixOfAnotherBinding()
        => Assert.Throws<ArgumentException>(() => new Keymap([KeyBinding.Parse("g", "a"), KeyBinding.Parse("gg", "b")]));

    [Fact]
    public void Keymap_RejectsALongerBindingOverAShorterOne()
        => Assert.Throws<ArgumentException>(() => new Keymap([KeyBinding.Parse("gg", "b"), KeyBinding.Parse("g", "a")]));

    [Fact]
    public void Keymap_RejectsADuplicate()
        => Assert.Throws<ArgumentException>(() => new Keymap([KeyBinding.Parse("x", "a"), KeyBinding.Parse("x", "b")]));

    [Fact]
    public void Keymap_BindingsFor_KeepsDeclarationOrder()
    {
        Keymap keymap = new([KeyBinding.Parse("j", "down"), KeyBinding.Parse("<Down>", "down"), KeyBinding.Parse("k", "up")]);

        Assert.Equal(["j", "<Down>"], keymap.BindingsFor("down").Select(binding => binding.Notation));
    }

    private KeyResolution Feed(string notation)
    {
        KeyResolution last = default;

        foreach (KeyStroke stroke in KeyNotation.Parse(notation))
        {
            last = _resolver.Feed(stroke);
        }

        return last;
    }
}
