using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Enigma.FileExplorer.Core.Operations;

/// <summary>
/// The Windows Recycle Bin, through the shell's <c>SHFileOperation</c> with undo allowed and every UI
/// suppressed: the same bin Explorer uses, restorable from it.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRecycleBin : ITrash
{
    private const uint FoDelete = 0x0003;
    private const ushort FofSilent = 0x0004;
    private const ushort FofNoConfirmation = 0x0010;
    private const ushort FofAllowUndo = 0x0040;
    private const ushort FofNoErrorUi = 0x0400;

    /// <inheritdoc />
    public bool CanTrash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // The shell has a bin only on fixed local drives; elsewhere "allow undo" silently deletes for good,
        // which must never happen behind the user's back.
        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(path));
            return !string.IsNullOrEmpty(root) && !root.StartsWith(@"\\", StringComparison.Ordinal)
                && (new DriveInfo(root).DriveType == DriveType.Fixed);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void MoveToTrash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string full = Path.GetFullPath(path);
        string name = Path.GetFileName(full);

        if (!CanTrash(full))
        {
            throw new FileOperationException($"'{name}' cannot go to the Recycle Bin on this drive");
        }

        if (!File.Exists(full) && !Directory.Exists(full))
        {
            throw new FileOperationException($"'{name}' does not exist");
        }

        // pFrom is a list of paths ended by an empty one: the marshaller adds one terminator, this adds the other.
        string from = full + '\0';
        ushort flags = FofAllowUndo | FofNoConfirmation | FofNoErrorUi | FofSilent;
        int result;
        bool aborted;

        if (Environment.Is64BitProcess)
        {
            ShFileOperation64 operation = new() { Function = FoDelete, From = from, Flags = flags };
            result = SHFileOperationW(ref operation);
            aborted = operation.AnyOperationsAborted;
        }
        else
        {
            ShFileOperation32 operation = new() { Function = FoDelete, From = from, Flags = flags };
            result = SHFileOperationW(ref operation);
            aborted = operation.AnyOperationsAborted;
        }

        if ((result != 0) || aborted)
        {
            throw new FileOperationException($"'{name}' could not be moved to the Recycle Bin (error 0x{result:X})");
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    private static extern int SHFileOperationW(ref ShFileOperation64 operation);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    private static extern int SHFileOperationW(ref ShFileOperation32 operation);

    /// <summary>
    /// SHFILEOPSTRUCTW as laid out in a 64-bit process (natural alignment).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileOperation64
    {
        public IntPtr Window;
        public uint Function;
        public string From;
        public string? To;
        public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool AnyOperationsAborted;
        public IntPtr NameMappings;
        public string? ProgressTitle;
    }

    /// <summary>
    /// SHFILEOPSTRUCTW as laid out in a 32-bit process: shellapi.h packs it to one byte there.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    private struct ShFileOperation32
    {
        public IntPtr Window;
        public uint Function;
        public string From;
        public string? To;
        public ushort Flags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool AnyOperationsAborted;
        public IntPtr NameMappings;
        public string? ProgressTitle;
    }
}
