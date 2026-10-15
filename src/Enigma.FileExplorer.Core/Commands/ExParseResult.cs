using System.Diagnostics.CodeAnalysis;

namespace Enigma.FileExplorer.Core.Commands;

/// <summary>
/// The outcome of parsing an ex command line: an invocation, or why there is none.
/// </summary>
public sealed class ExParseResult
{
    private ExParseResult(ExInvocation? invocation, string? error)
    {
        Invocation = invocation;
        Error = error;
    }

    /// <summary>
    /// The invocation, when the line parsed.
    /// </summary>
    public ExInvocation? Invocation { get; }

    /// <summary>
    /// Why the line did not parse, in vim's terse style.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Whether the line parsed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Invocation))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Invocation is not null;

    /// <summary>
    /// A successful parse.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The result.</returns>
    public static ExParseResult Success(ExInvocation invocation) => new(invocation, null);

    /// <summary>
    /// A failed parse.
    /// </summary>
    /// <param name="error">Why.</param>
    /// <returns>The result.</returns>
    public static ExParseResult Failure(string error) => new(null, error);
}
