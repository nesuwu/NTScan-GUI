using System.Runtime.InteropServices;

namespace NTScanGUI.Interop;

internal static partial class NtScanNative
{
    [LibraryImport(
        "ntscan.dll",
        EntryPoint = "ntscan_scan_directory_json",
        StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr ScanDirectoryJsonNative(
        string path,
        uint mode,
        [MarshalAs(UnmanagedType.I1)] bool followSymlinks,
        [MarshalAs(UnmanagedType.I1)] bool showFiles,
        string? cachePath);

    [LibraryImport(
        "ntscan.dll",
        EntryPoint = "ntscan_find_duplicates_json",
        StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr FindDuplicatesJsonNative(
        string path,
        ulong minSize,
        string? hashCachePath);

    [LibraryImport("ntscan.dll", EntryPoint = "ntscan_free_string")]
    private static partial void FreeStringNative(IntPtr ptr);

    public static string ScanDirectoryJson(
        string path,
        uint mode,
        bool followSymlinks,
        bool showFiles,
        string? cachePath)
        => InvokeUtf8(() => ScanDirectoryJsonNative(path, mode, followSymlinks, showFiles, cachePath));

    public static string FindDuplicatesJson(string path, ulong minSize, string? hashCachePath)
        => InvokeUtf8(() => FindDuplicatesJsonNative(path, minSize, hashCachePath));

    private static string InvokeUtf8(Func<IntPtr> nativeCall)
    {
        IntPtr payloadPtr = nativeCall();
        if (payloadPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Native API returned a null payload pointer.");
        }

        try
        {
            return Marshal.PtrToStringUTF8(payloadPtr)
                ?? throw new InvalidOperationException("Native API returned an unreadable UTF-8 payload.");
        }
        finally
        {
            FreeStringNative(payloadPtr);
        }
    }
}
