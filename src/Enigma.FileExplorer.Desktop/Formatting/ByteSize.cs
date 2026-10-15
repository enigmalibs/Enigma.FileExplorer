using System;
using System.Globalization;

namespace Enigma.FileExplorer.Desktop.Formatting;

/// <summary>
/// Formats byte counts for people: <c>512 B</c>, <c>1.4 KB</c>, <c>23 MB</c>.
/// </summary>
/// <remarks>
/// Units are powers of 1,024 with the familiar KB/MB/GB labels, as Windows Explorer and most Linux
/// file managers show them. One decimal below 10 of a unit, none above, so a column of sizes stays
/// narrow and readable.
/// </remarks>
public static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <summary>
    /// Formats a byte count.
    /// </summary>
    /// <param name="bytes">The count; negative values are formatted as 0.</param>
    /// <param name="culture">The culture for the decimal separator; the current culture when omitted.</param>
    /// <returns>The formatted size.</returns>
    public static string Format(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (bytes < 1024)
        {
            return string.Create(culture, $"{Math.Max(bytes, 0)} B");
        }

        double value = bytes;
        int unit = 0;

        while ((value >= 1024) && (unit < Units.Length - 1))
        {
            value /= 1024;
            unit++;
        }

        string number = value < 10
            ? value.ToString("0.#", culture)
            : Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", culture);

        return $"{number} {Units[unit]}";
    }
}
