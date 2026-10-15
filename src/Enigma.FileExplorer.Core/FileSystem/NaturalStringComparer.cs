using System;
using System.Collections.Generic;
using System.Globalization;

namespace Enigma.FileExplorer.Core.FileSystem;

/// <summary>
/// Compares names the way people read them: case-insensitively, with runs of digits compared as
/// numbers, so <c>file2</c> sorts before <c>file10</c>.
/// </summary>
/// <remarks>
/// Text runs are compared with the invariant culture, ignoring case, so accented letters sort next to
/// their base letters and the order does not change with the machine's culture. Names that compare
/// equal that way (<c>A</c> and <c>a</c>, <c>01</c> and <c>1</c>) fall back to an ordinal comparison, so
/// the order is total and stable.
/// </remarks>
public sealed class NaturalStringComparer : IComparer<string>
{
    private static readonly CompareInfo Culture = CultureInfo.InvariantCulture.CompareInfo;

    private NaturalStringComparer()
    {
    }

    /// <summary>
    /// The shared instance; the comparer holds no state.
    /// </summary>
    public static NaturalStringComparer Instance { get; } = new();

    /// <inheritdoc />
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int natural = CompareNatural(x, y);
        return natural != 0 ? natural : string.CompareOrdinal(x, y);
    }

    private static int CompareNatural(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        while (!x.IsEmpty && !y.IsEmpty)
        {
            bool xDigit = char.IsAsciiDigit(x[0]);
            bool yDigit = char.IsAsciiDigit(y[0]);

            if (xDigit != yDigit)
            {
                // A number sorts before text, as it does in an ordinal comparison.
                return xDigit ? -1 : 1;
            }

            int xRun = RunLength(x, xDigit);
            int yRun = RunLength(y, yDigit);

            int result = xDigit
                ? CompareNumbers(x[..xRun], y[..yRun])
                : Culture.Compare(x[..xRun], y[..yRun], CompareOptions.IgnoreCase);

            if (result != 0)
            {
                return result;
            }

            x = x[xRun..];
            y = y[yRun..];
        }

        return x.Length.CompareTo(y.Length);
    }

    private static int RunLength(ReadOnlySpan<char> text, bool digits)
    {
        int length = 1;

        while ((length < text.Length) && (char.IsAsciiDigit(text[length]) == digits))
        {
            length++;
        }

        return length;
    }

    private static int CompareNumbers(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        // Compared as digit strings, not parsed, so a 40-digit run cannot overflow anything.
        ReadOnlySpan<char> xValue = x.TrimStart('0');
        ReadOnlySpan<char> yValue = y.TrimStart('0');

        if (xValue.Length != yValue.Length)
        {
            return xValue.Length.CompareTo(yValue.Length);
        }

        return xValue.SequenceCompareTo(yValue);
    }
}
