using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.UI.Xaml.Input;
using NTScanGUI.Models;
using NTScanGUI.Services;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace NTScanGUI.Views;

public sealed partial class ScanPage : Page
{
    private readonly NtScanApiClient apiClient = new();
    private readonly ObservableCollection<ScanEntryRow> scanEntries = [];
    private readonly ObservableCollection<string> breadcrumbSegments = [];
    private readonly Stack<string> backStack = new();

    private string currentPath = string.Empty;
    private bool isBusy;
    private int scanElapsedSeconds;
    private Microsoft.UI.Xaml.DispatcherTimer? scanTimer;

    public ScanPage()
    {
        this.InitializeComponent();
        EntriesListView.ItemsSource = scanEntries;
        PathBreadcrumb.ItemsSource = breadcrumbSegments;

        // Load settings defaults.
        var s = SettingsService.Instance;
        AccurateModeToggle.IsChecked = s.DefaultScanMode == 1;
        FollowSymlinksToggle.IsChecked = s.DefaultFollowSymlinks;
        ShowFilesToggle.IsChecked = s.DefaultShowFiles;

        // Navigate to user profile on first load.
        string startPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        this.Loaded += (_, _) => NavigateTo(startPath);
    }

    // ── Navigation ──────────────────────────────────────────────────

    private async void NavigateTo(string path, bool addToHistory = true)
    {
        path = Path.GetFullPath(path);
        if (isBusy) return;
        if (!Directory.Exists(path))
        {
            StatusTextBlock.Text = $"Directory not found: {path}";
            return;
        }

        if (addToHistory && !string.IsNullOrEmpty(currentPath))
        {
            backStack.Push(currentPath);
        }

        currentPath = path;
        UpdateBreadcrumb();
        BackButton.IsEnabled = backStack.Count > 0;
        UpButton.IsEnabled = Directory.GetParent(currentPath) is not null;

        // ① Show filesystem listing immediately (before scan).
        PopulatePreScanListing();

        // ② Run the full NTScan analysis in the background.
        await RunScanAsync();
    }

    // ── Pre-scan instant listing ────────────────────────────────────

    private void PopulatePreScanListing()
    {
        scanEntries.Clear();

        try
        {
            var dirInfo = new DirectoryInfo(currentPath);
            var entries = new List<ScanEntryRow>();

            foreach (var fsi in dirInfo.EnumerateFileSystemInfos())
            {
                bool isDir = fsi.Attributes.HasFlag(FileAttributes.Directory);
                string modified = "—";
                try { modified = fsi.LastWriteTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); }
                catch { /* skip */ }

                string sizeStr = "";
                if (!isDir)
                {
                    try { sizeStr = FormatBytes((ulong)((FileInfo)fsi).Length); }
                    catch { sizeStr = "—"; }
                }

                entries.Add(new ScanEntryRow
                {
                    Name = fsi.Name,
                    Kind = isDir ? "Directory" : "File",
                    LogicalSize = sizeStr,
                    AllocatedSize = "",
                    Ads = "",
                    Percent = "",
                    Modified = modified,
                    Notes = "",
                    FullPath = fsi.FullName,
                    IsDirectory = isDir,
                    IconGlyph = isDir ? "\uE8B7" : "\uE8A5",
                });
            }

            // Directories first, then files; alphabetical within each group.
            foreach (var row in entries
                         .OrderByDescending(e => e.IsDirectory)
                         .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                scanEntries.Add(row);
            }

            StatusTextBlock.Text = $"{scanEntries.Count} items";
        }
        catch (UnauthorizedAccessException)
        {
            StatusTextBlock.Text = "Access denied.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error listing directory: {ex.Message}";
        }
    }

    private void UpdateBreadcrumb()
    {
        breadcrumbSegments.Clear();

        string root = Path.GetPathRoot(currentPath) ?? string.Empty;
        breadcrumbSegments.Add(root.TrimEnd(Path.DirectorySeparatorChar));

        string relative = Path.GetRelativePath(root, currentPath);
        if (relative != ".")
        {
            foreach (string segment in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                breadcrumbSegments.Add(segment);
            }
        }
    }

    // ── Scanning ────────────────────────────────────────────────────

    private async Task RunScanAsync()
    {
        NativeScanMode mode = AccurateModeToggle.IsChecked == true
            ? NativeScanMode.Accurate
            : NativeScanMode.Fast;
        bool followSymlinks = FollowSymlinksToggle.IsChecked == true;
        bool showFiles = ShowFilesToggle.IsChecked == true;

        var s = SettingsService.Instance;
        string? cachePath = string.IsNullOrWhiteSpace(s.ScanCachePath) ? null : s.ScanCachePath;

        SetBusy(true, "Scanning…");
        StartScanTimer();

        try
        {
            DirectoryReportDto report = await Task.Run(
                () => apiClient.ScanDirectory(currentPath, mode, followSymlinks, showFiles, cachePath));
            BindScanReport(report);
            StatusTextBlock.Text =
                $"{scanEntries.Count} items  •  " +
                $"Total: {FormatBytes(report.LogicalSize)}  •  " +
                $"Allocated: {FormatAllocated(report.AllocatedSize, report.AllocatedComplete)}  •  " +
                $"Scanned in {scanElapsedSeconds}s";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Scan failed: {ex.Message}";
        }
        finally
        {
            StopScanTimer();
            SetBusy(false);
        }
    }

    // ── Scan timer ──────────────────────────────────────────────────

    private void StartScanTimer()
    {
        scanElapsedSeconds = 0;
        scanTimer = new Microsoft.UI.Xaml.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        scanTimer.Tick += (_, _) =>
        {
            scanElapsedSeconds++;
            StatusTextBlock.Text = $"Scanning… {scanElapsedSeconds}s";
        };
        scanTimer.Start();
    }

    private void StopScanTimer()
    {
        scanTimer?.Stop();
        scanTimer = null;
    }

    // ── Event handlers ──────────────────────────────────────────────

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (backStack.Count > 0)
        {
            string previous = backStack.Pop();
            NavigateTo(previous, addToHistory: false);
        }
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        var parent = Directory.GetParent(currentPath);
        if (parent is not null)
        {
            NavigateTo(parent.FullName);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(currentPath, addToHistory: false);
    }

    private async void BrowseTargetPath_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow is null) return;

        FolderPicker folderPicker = new();
        folderPicker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(folderPicker, WindowNative.GetWindowHandle(App.MainWindow));

        var selectedFolder = await folderPicker.PickSingleFolderAsync();
        if (selectedFolder is not null)
        {
            NavigateTo(selectedFolder.Path);
        }
    }

    private void EntriesListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ScanEntryRow row && row.IsDirectory)
        {
            NavigateTo(row.FullPath);
        }
    }

    private void PathBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        int clickedIndex = args.Index;
        string root = breadcrumbSegments[0] + Path.DirectorySeparatorChar;

        if (clickedIndex == 0)
        {
            NavigateTo(root);
            return;
        }

        string rebuilt = Path.Combine(root,
            string.Join(Path.DirectorySeparatorChar.ToString(),
                breadcrumbSegments.Skip(1).Take(clickedIndex)));

        NavigateTo(rebuilt);
    }

    private void AddressBar_Tapped(object sender, TappedRoutedEventArgs e)
    {
        PathBreadcrumb.Visibility = Visibility.Collapsed;
        AddressTextBox.Visibility = Visibility.Visible;
        AddressTextBox.Text = currentPath;
        AddressTextBox.SelectAll();
        AddressTextBox.Focus(FocusState.Programmatic);
    }

    private void AddressTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            CommitAddressBar();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Escape)
        {
            DismissAddressBar();
            e.Handled = true;
        }
    }

    private void AddressTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        DismissAddressBar();
    }

    private void CommitAddressBar()
    {
        string newPath = AddressTextBox.Text.Trim();
        DismissAddressBar();

        if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
        {
            NavigateTo(newPath);
        }
        else
        {
            StatusTextBlock.Text = $"Directory not found: {newPath}";
        }
    }

    private void DismissAddressBar()
    {
        AddressTextBox.Visibility = Visibility.Collapsed;
        PathBreadcrumb.Visibility = Visibility.Visible;
    }

    private async void FindDuplicates_Click(object sender, RoutedEventArgs e)
    {
        if (isBusy || string.IsNullOrEmpty(currentPath)) return;

        var s = SettingsService.Instance;
        ulong minSize = (ulong)Math.Max(0, s.DefaultMinDuplicateSize);
        string? hashCachePath = string.IsNullOrWhiteSpace(s.HashCachePath) ? null : s.HashCachePath;

        SetBusy(true, "Searching for duplicates…");
        StartScanTimer();

        try
        {
            DuplicateScanResultDto result = await Task.Run(
                () => apiClient.FindDuplicates(currentPath, minSize, hashCachePath));

            string msg = $"Files scanned: {result.TotalFilesScanned}\n" +
                         $"Duplicate groups: {result.Groups.Count}\n" +
                         $"Reclaimable: {FormatBytes(result.TotalReclaimable)}\n" +
                         $"Time: {scanElapsedSeconds}s";

            if (result.Groups.Count > 0)
            {
                var top = result.Groups
                    .OrderByDescending(g => g.ReclaimableBytes)
                    .Take(20)
                    .Select(g => $"{g.HashHex[..Math.Min(12, g.HashHex.Length)]}…  {FormatBytes(g.Size)}  ×{g.Paths.Count}  ({FormatBytes(g.ReclaimableBytes)} reclaimable)");
                msg += "\n\nTop duplicate groups:\n" + string.Join("\n", top);
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = "Duplicate Scan Results",
                Content = new ScrollViewer
                {
                    MaxHeight = 500,
                    Content = new TextBlock
                    {
                        Text = msg,
                        TextWrapping = TextWrapping.Wrap,
                        IsTextSelectionEnabled = true,
                    }
                },
                CloseButtonText = "Close",
            };
            await dialog.ShowAsync();

            StatusTextBlock.Text = $"Found {result.Groups.Count} duplicate groups, {FormatBytes(result.TotalReclaimable)} reclaimable.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Duplicate search failed: {ex.Message}";
        }
        finally
        {
            StopScanTimer();
            SetBusy(false);
        }
    }

    // ── Binding ─────────────────────────────────────────────────────

    private void BindScanReport(DirectoryReportDto report)
    {
        scanEntries.Clear();

        var ordered = report.Entries
            .OrderByDescending(e => IsDirectoryKind(e.Kind))
            .ThenByDescending(e => e.LogicalSize);

        foreach (EntryReportDto entry in ordered)
        {
            bool isDir = IsDirectoryKind(entry.Kind);
            scanEntries.Add(new ScanEntryRow
            {
                Name = entry.Name,
                Kind = entry.Kind,
                LogicalSize = FormatBytes(entry.LogicalSize),
                AllocatedSize = FormatAllocated(entry.AllocatedSize, entry.AllocatedComplete),
                Ads = entry.AdsCount > 0
                    ? $"{entry.AdsCount} ({FormatBytes(entry.AdsBytes)})"
                    : "—",
                Percent = entry.PercentOfParent.ToString("0.00", CultureInfo.InvariantCulture),
                Modified = FormatUnixSeconds(entry.ModifiedUnixSeconds),
                Notes = entry.Error ?? entry.SkipReason ?? "—",
                FullPath = entry.Path,
                IsDirectory = isDir,
                IconGlyph = isDir ? "\uE8B7" : "\uE8A5",
            });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static bool IsDirectoryKind(string kind)
        => kind is "Directory" or "SymlinkDirectory";

    private void SetBusy(bool busy, string? status = null)
    {
        isBusy = busy;
        ScanProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        BackButton.IsEnabled = !busy && backStack.Count > 0;
        UpButton.IsEnabled = !busy && Directory.GetParent(currentPath) is not null;
        RefreshButton.IsEnabled = !busy;
        FindDuplicatesButton.IsEnabled = !busy;

        if (status is not null)
            StatusTextBlock.Text = status;
    }

    private static string FormatAllocated(ulong? allocated, bool complete)
    {
        if (!allocated.HasValue) return "n/a";
        return complete
            ? FormatBytes(allocated.Value)
            : $"{FormatBytes(allocated.Value)} (partial)";
    }

    private static string FormatUnixSeconds(long? unixSeconds)
    {
        if (!unixSeconds.HasValue) return "—";
        try
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(unixSeconds.Value)
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }
        catch { return "—"; }
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB", "PiB"];
        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.00} {units[unitIndex]}";
    }
}
