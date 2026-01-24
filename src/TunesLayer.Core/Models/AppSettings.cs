namespace TunesLayer.Core.Models;

public class AppSettings
{
    // General
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
    public bool ShowOverlayOnStartup { get; set; } = true;

    // Overlay
    public double OverlayOpacity { get; set; } = 0.9;
    public double OverlaySize { get; set; } = 250;
    public double OverlayX { get; set; } = 50;
    public double OverlayY { get; set; } = 50;
    public bool ClickThroughEnabled { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;

    // Hotkeys
    public string HotkeyPlayPause { get; set; } = "Ctrl+Alt+Space";
    public string HotkeyNext { get; set; } = "Ctrl+Alt+Right";
    public string HotkeyPrevious { get; set; } = "Ctrl+Alt+Left";
    public string HotkeyVolumeUp { get; set; } = "Ctrl+Alt+Up";
    public string HotkeyVolumeDown { get; set; } = "Ctrl+Alt+Down";
    public string HotkeyToggleOverlay { get; set; } = "Ctrl+Alt+O";
    public string HotkeyToggleClickThrough { get; set; } = "Ctrl+Alt+C";

    // Theme
    public string SelectedTheme { get; set; } = "midnight";

    // Integrations
    public bool DiscordEnabled { get; set; }
    public bool OBSWidgetEnabled { get; set; }
    public int OBSWidgetPort { get; set; } = 5123;

    // Analytics
    public bool AnalyticsEnabled { get; set; } = true;
    public int AnalyticsRetentionDays { get; set; } = 30;
}
