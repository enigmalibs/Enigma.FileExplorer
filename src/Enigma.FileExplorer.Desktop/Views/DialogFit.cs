using System;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Enigma.FileExplorer.Desktop.Views;

/// <summary>
/// Keeps a dialog's own scroll area from making the Enigma dialog card scroll as well: two nested scroll
/// areas fight over the wheel and the bar.
/// </summary>
internal static class DialogFit
{
    /// <summary>
    /// Caps a scroll area's height so the dialog card around it does not overflow in this window: as tall as
    /// it may be, less whatever the card would otherwise overflow by, but never below a floor (in a very
    /// short window the card may then scroll a little).
    /// </summary>
    /// <param name="scrollArea">The content's own scroll area (or list).</param>
    /// <param name="tallest">The tallest it may be.</param>
    /// <param name="shortest">The floor.</param>
    public static void FitToCard(Control scrollArea, double tallest, double shortest)
    {
        ArgumentNullException.ThrowIfNull(scrollArea);

        if (scrollArea.FindAncestorOfType<ScrollViewer>() is not { } card)
        {
            return;
        }

        scrollArea.MaxHeight = tallest;
        card.UpdateLayout();

        double overflow = card.Extent.Height - card.Viewport.Height;

        if (overflow > 0.5)
        {
            scrollArea.MaxHeight = Math.Max(shortest, tallest - Math.Ceiling(overflow));
            card.UpdateLayout();
        }
    }
}
