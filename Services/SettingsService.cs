using System.Text.Json;
using System.Text.Json.Serialization;

namespace NTScanGUI.Services;

internal sealed class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTScanGUI");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static SettingsService? _instance;

    /// <summary>Singleton accessor. Call <see cref="Load"/> once at startup.</summary>
    public static SettingsService Instance => _instance ??= Load();

    // ── Appearance ──────────────────────────────────────────────────
    public string AppTheme { get; set; } = "System";

    // ── Scan Defaults ───────────────────────────────────────────────
    public int DefaultScanMode { get; set; } // 0 = Fast, 1 = Accurate
    public bool DefaultFollowSymlinks { get; set; }
    public bool DefaultShowFiles { get; set; }
    public double DefaultMinDuplicateSize { get; set; } = 1_048_576; // 1 MiB

    // ── Cache Paths ─────────────────────────────────────────────────
    public string? ScanCachePath { get; set; }
    public string? HashCachePath { get; set; }

    // ── Persistence ─────────────────────────────────────────────────

    public static SettingsService Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<SettingsService>(json, JsonOpts);
                if (loaded is not null)
                {
                    _instance = loaded;
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt file → fall through to defaults.
        }

        _instance = new SettingsService();
        return _instance;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Best-effort; settings are non-critical.
        }
    }
}
