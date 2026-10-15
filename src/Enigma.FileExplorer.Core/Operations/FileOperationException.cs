using System;
using System.IO;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// A file operation that failed, with a message meant for the user ("'report.pdf' already exists").
/// </summary>
public sealed class FileOperationException : IOException
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    public FileOperationException()
    {
    }

    /// <summary>
    /// Creates the exception with a user-facing message.
    /// </summary>
    /// <param name="message">What went wrong, for the user.</param>
    public FileOperationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates the exception with a user-facing message and the failure behind it.
    /// </summary>
    /// <param name="message">What went wrong, for the user.</param>
    /// <param name="innerException">The underlying failure.</param>
    public FileOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
