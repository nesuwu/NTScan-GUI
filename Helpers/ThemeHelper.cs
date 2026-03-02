using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace NTScanGUI.Helpers;

internal static class ThemeHelper
{
    /// <summary>
    /// Applies the requested theme string ("Light", "Dark", or "System") to the
    /// given root element. When "System" is chosen the current OS setting is read
    /// from <see cref="UISettings"/>.
    /// </summary>
    public static void ApplyTheme(FrameworkElement root, string themeName)
    {
        root.RequestedTheme = themeName switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ResolveSystemTheme(),
        };
    }

    private static ElementTheme ResolveSystemTheme()
    {
        var uiSettings = new UISettings();
        var fg = uiSettings.GetColorValue(UIColorType.Foreground);

        // Foreground is light (close to white) → OS is in dark mode.
        bool isDark = fg.R > 128 && fg.G > 128 && fg.B > 128;
        return isDark ? ElementTheme.Dark : ElementTheme.Light;
    }
}
