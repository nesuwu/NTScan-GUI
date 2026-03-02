using System.Text.Json;
using NTScanGUI.Interop;
using NTScanGUI.Models;

namespace NTScanGUI.Services;

internal sealed class NtScanApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public DirectoryReportDto ScanDirectory(
        string path,
        NativeScanMode mode,
        bool followSymlinks,
        bool showFiles,
        string? cachePath)
    {
        string payload = NtScanNative.ScanDirectoryJson(path, (uint)mode, followSymlinks, showFiles, cachePath);
        FfiEnvelope<DirectoryReportDto> envelope = Deserialize<FfiEnvelope<DirectoryReportDto>>(payload);
        return Unwrap(envelope);
    }

    public DuplicateScanResultDto FindDuplicates(string path, ulong minSize, string? hashCachePath)
    {
        string payload = NtScanNative.FindDuplicatesJson(path, minSize, hashCachePath);
        FfiEnvelope<DuplicateScanResultDto> envelope = Deserialize<FfiEnvelope<DuplicateScanResultDto>>(payload);
        return Unwrap(envelope);
    }

    private static T Deserialize<T>(string payload)
    {
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Native API returned an empty JSON response.");
    }

    private static T Unwrap<T>(FfiEnvelope<T> envelope)
        where T : class
    {
        if (!envelope.Ok)
        {
            throw new InvalidOperationException(envelope.Error ?? "Native API returned an unknown error.");
        }

        return envelope.Data
            ?? throw new InvalidOperationException("Native API reported success but returned no data.");
    }
}
