using NTScanGUI.Services;

namespace NTScanGUI.Views;

public sealed partial class SettingsPage : Page
{
    private bool _isLoading = true;

    public SettingsPage()
    {
        this.InitializeComponent();
        LoadCurrentValues();
        _isLoading = false;
    }

    // ── Initialisation ──────────────────────────────────────────────

    private void LoadCurrentValues()
    {
        var s = SettingsService.Instance;

        // Theme
        int themeIndex = s.AppTheme switch
        {
            "Light" => 0,
            "Dark" => 1,
            _ => 2,
        };
        ThemeComboBox.SelectedIndex = themeIndex;

        // Scan defaults
        ScanModeComboBox.SelectedIndex = s.DefaultScanMode;
        FollowSymlinksToggle.IsOn = s.DefaultFollowSymlinks;
        ShowFilesToggle.IsOn = s.DefaultShowFiles;
        MinDuplicateSizeBox.Value = s.DefaultMinDuplicateSize;

        // Cache paths
        ScanCachePathTextBox.Text = s.ScanCachePath ?? string.Empty;
        HashCachePathTextBox.Text = s.HashCachePath ?? string.Empty;
    }

    // ── Event handlers (auto-save on change) ────────────────────────

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            SettingsService.Instance.AppTheme = tag;
            SettingsService.Instance.Save();

            // Live-apply theme through the shell.
            if (App.MainWindow?.Content is Frame { Content: ShellPage shell })
            {
                shell.RefreshTheme();
            }
        }
    }

    private void ScanModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        SettingsService.Instance.DefaultScanMode = ScanModeComboBox.SelectedIndex;
        SettingsService.Instance.Save();
    }

    private void FollowSymlinksToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        SettingsService.Instance.DefaultFollowSymlinks = FollowSymlinksToggle.IsOn;
        SettingsService.Instance.Save();
    }

    private void ShowFilesToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        SettingsService.Instance.DefaultShowFiles = ShowFilesToggle.IsOn;
        SettingsService.Instance.Save();
    }

    private void MinDuplicateSizeBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading) return;
        if (!double.IsNaN(args.NewValue))
        {
            SettingsService.Instance.DefaultMinDuplicateSize = args.NewValue;
            SettingsService.Instance.Save();
        }
    }

    private void CachePath_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        var s = SettingsService.Instance;
        s.ScanCachePath = string.IsNullOrWhiteSpace(ScanCachePathTextBox.Text) ? null : ScanCachePathTextBox.Text.Trim();
        s.HashCachePath = string.IsNullOrWhiteSpace(HashCachePathTextBox.Text) ? null : HashCachePathTextBox.Text.Trim();
        s.Save();
    }
}
