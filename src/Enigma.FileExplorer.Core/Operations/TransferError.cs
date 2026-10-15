namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// One item that could not be transferred.
/// </summary>
/// <param name="Path">The source.</param>
/// <param name="Message">Why, for the user.</param>
public sealed record TransferError(string Path, string Message);
