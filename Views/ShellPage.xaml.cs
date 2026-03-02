using NTScanGUI.Helpers;
using NTScanGUI.Services;

namespace NTScanGUI.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();

        // Apply saved theme.
        ThemeHelper.ApplyTheme(this, SettingsService.Instance.AppTheme);

        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set up custom titlebar drag region (must be done after the page is loaded).
        if (App.MainWindow is not null)
        {
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.SetTitleBar(AppTitleBar);
        }

        // Select the first nav item (Scan) by default.
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item &&
            item.Tag is string tag)
        {
            var pageType = tag switch
            {
                "ScanPage" => typeof(ScanPage),
                _ => typeof(ScanPage),
            };

            ContentFrame.Navigate(pageType);
        }
    }

    /// <summary>
    /// Called by SettingsPage after the user changes the theme.
    /// </summary>
    public void RefreshTheme()
    {
        ThemeHelper.ApplyTheme(this, SettingsService.Instance.AppTheme);
    }
}
