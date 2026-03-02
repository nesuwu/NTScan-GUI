using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NTScanGUI.Services;

namespace NTScanGUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        // Load persisted settings.
        _ = SettingsService.Instance;

        MainWindow ??= new Window();

        // Apply Mica backdrop for native Windows 11 feel.
        MainWindow.SystemBackdrop = new MicaBackdrop();

        // Set a reasonable default window size.
        var appWindow = MainWindow.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 820));

        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            MainWindow.Content = rootFrame;
        }

        _ = rootFrame.Navigate(typeof(Views.ShellPage), e.Arguments);
        MainWindow.Activate();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[NTScan] Unhandled: {e.Exception}");
        e.Handled = false; // Keeps the default crash behavior but logs first.
    }

    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }
}
