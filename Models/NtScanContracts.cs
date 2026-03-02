using System.Text.Json.Serialization;

namespace NTScanGUI.Models;

internal sealed class FfiEnvelope<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

internal sealed class DirectoryReportDto
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("modified_unix_secs")]
    public long? ModifiedUnixSeconds { get; init; }

    [JsonPropertyName("logical_size")]
    public ulong LogicalSize { get; init; }

    [JsonPropertyName("allocated_size")]
    public ulong? AllocatedSize { get; init; }

    [JsonPropertyName("allocated_complete")]
    public bool AllocatedComplete { get; init; }

    [JsonPropertyName("entries")]
    public List<EntryReportDto> Entries { get; init; } = [];
}

internal sealed class EntryReportDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("logical_size")]
    public ulong LogicalSize { get; init; }

    [JsonPropertyName("allocated_size")]
    public ulong? AllocatedSize { get; init; }

    [JsonPropertyName("allocated_complete")]
    public bool AllocatedComplete { get; init; }

    [JsonPropertyName("percent_of_parent")]
    public double PercentOfParent { get; init; }

    [JsonPropertyName("ads_bytes")]
    public ulong AdsBytes { get; init; }

    [JsonPropertyName("ads_count")]
    public int AdsCount { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("skip_reason")]
    public string? SkipReason { get; init; }

    [JsonPropertyName("modified_unix_secs")]
    public long? ModifiedUnixSeconds { get; init; }
}

internal sealed class DuplicateScanResultDto
{
    [JsonPropertyName("total_files_scanned")]
    public ulong TotalFilesScanned { get; init; }

    [JsonPropertyName("total_reclaimable")]
    public ulong TotalReclaimable { get; init; }

    [JsonPropertyName("groups")]
    public List<DuplicateGroupDto> Groups { get; init; } = [];
}

internal sealed class DuplicateGroupDto
{
    [JsonPropertyName("hash_hex")]
    public string HashHex { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public ulong Size { get; init; }

    [JsonPropertyName("reclaimable_bytes")]
    public ulong ReclaimableBytes { get; init; }

    [JsonPropertyName("paths")]
    public List<string> Paths { get; init; } = [];
}

internal sealed class ScanEntryRow
{
    public required string Name { get; init; }

    public required string Kind { get; init; }

    public required string LogicalSize { get; init; }

    public required string AllocatedSize { get; init; }

    public required string Ads { get; init; }

    public required string Percent { get; init; }

    public required string Modified { get; init; }

    public required string Notes { get; init; }

    public required string FullPath { get; init; }

    public bool IsDirectory { get; init; }

    public string IconGlyph { get; init; } = "\uE8A5";
}

internal sealed class DuplicateGroupRow
{
    public required string Hash { get; init; }

    public required string FileSize { get; init; }

    public required string Reclaimable { get; init; }

    public required string Count { get; init; }

    public required string Paths { get; init; }
}

internal enum NativeScanMode : uint
{
    Fast = 0,
    Accurate = 1,
}
