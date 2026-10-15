using System;
using System.Reflection;

namespace Enigma.FileExplorer.Core.Diagnostics;

/// <summary>
/// Identity of the Enigma.FileExplorer product: its names, and which build of it is running.
/// </summary>
public static class ProductInformation
{
    /// <summary>
    /// The product's technical name, as the window title shows it.
    /// </summary>
    public const string Name = "Enigma.FileExplorer";

    /// <summary>
    /// The product's name in words, as the About dialog presents it.
    /// </summary>
    /// <remarks>
    /// Beside <see cref="Name"/> rather than in place of it: the window title keeps the technical name.
    /// </remarks>
    public const string DisplayName = "Enigma File Explorer";

    /// <summary>
    /// The version and the revision this build was cut from, read once from this assembly.
    /// </summary>
    /// <remarks>
    /// From <b>this</b> assembly rather than the entry assembly: Core and Desktop carry one version
    /// between them (<c>Directory.Build.props</c>), and under a test host the entry assembly is the runner.
    /// </remarks>
    private static readonly ProductVersion Product = ProductVersion.From(
        typeof(ProductInformation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        FallbackVersion());

    /// <summary>
    /// Gets the displayable version, without the source revision the SDK appends.
    /// </summary>
    public static string Version => Product.Version;

    /// <summary>
    /// Gets the short commit this build was cut from, or <see langword="null"/> when it recorded none:
    /// a build from a source drop rather than from the repository.
    /// </summary>
    public static string? BuildSha => Product.BuildSha;

    /// <summary>
    /// Gets the copyright notice <c>Directory.Build.props</c> stamps on the assembly, or an empty string
    /// when it carries none.
    /// </summary>
    public static string Copyright { get; } =
        typeof(ProductInformation).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright?.Trim() ?? string.Empty;

    /// <summary>
    /// The plain assembly version, for when the informational one says nothing usable, trimmed to
    /// three components: the fourth is a zero nobody wrote.
    /// </summary>
    private static string? FallbackVersion()
    {
        Version? version = typeof(ProductInformation).Assembly.GetName().Version;

        return version is null ? null : version.ToString(version.Build >= 0 ? 3 : 2);
    }
}
