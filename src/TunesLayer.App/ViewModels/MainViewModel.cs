using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TunesLayer.Core.Models;
using TunesLayer.Core.Services;
using TunesLayer.Overlay;
using TunesLayer.Integrations.Discord;

namespace TunesLayer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IOverlayManager _overlayManager;
    private readonly IThemeService _themeService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IMediaSessionService _mediaService;
    private readonly IDiscordRpcService _discordService;
    private bool _isLoadingSettings;

    public MainViewModel()
    {
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _overlayManager = App.Services.GetRequiredService<IOverlayManager>();
        _themeService = App.Services.GetRequiredService<IThemeService>();
        _analyticsService = App.Services.GetRequiredService<IAnalyticsService>();
        _hotkeyService = App.Services.GetRequiredService<IHotkeyService>();
        _mediaService = App.Services.GetRequiredService<IMediaSessionService>();
        _discordService = App.Services.GetRequiredService<IDiscordRpcService>();

        _isLoadingSettings = true;
        LoadSettings();
        LoadThemes();
        LoadAnalytics();
        _isLoadingSettings = false;
        
        // Poll for settings changes (e.g., from hotkeys) every 500ms
        var syncTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        syncTimer.Tick += (s, e) => SyncSettingsFromService();
        syncTimer.Start();
    }
    
    private void SyncSettingsFromService()
    {
        // Sync click-through state from overlay manager (updated by hotkey)
        if (_overlayManager.IsClickThrough != ClickThroughEnabled)
        {
            _isLoadingSettings = true;
            ClickThroughEnabled = _overlayManager.IsClickThrough;
            _isLoadingSettings = false;
        }
    }

    // General Settings
    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _showOverlayOnStartup = true;

    // Overlay Settings
    [ObservableProperty]
    private double _overlayOpacity = 0.9;

    [ObservableProperty]
    private double _overlaySize = 250;

    [ObservableProperty]
    private bool _clickThroughEnabled = true;

    [ObservableProperty]
    private bool _showNotifications = true;

    // Hotkey Settings
    [ObservableProperty]
    private string _hotkeyPlayPause = "Ctrl+Alt+Space";

    [ObservableProperty]
    private string _hotkeyNext = "Ctrl+Alt+Right";

    [ObservableProperty]
    private string _hotkeyPrevious = "Ctrl+Alt+Left";

    [ObservableProperty]
    private string _hotkeyToggleOverlay = "Ctrl+Alt+O";

    [ObservableProperty]
    private string _hotkeyToggleClickThrough = "Ctrl+Alt+C";

    // Theme Settings
    [ObservableProperty]
    private ObservableCollection<ThemeInfo> _availableThemes = new();

    [ObservableProperty]
    private ThemeInfo? _selectedTheme;

    // Integration Settings
    [ObservableProperty]
    private bool _discordEnabled;

    [ObservableProperty]
    private bool _oBSWidgetEnabled;

    // Analytics
    [ObservableProperty]
    private string _todayListeningTime = "0h 0m";

    [ObservableProperty]
    private int _tracksPlayedToday;

    private void LoadSettings()
    {
        var settings = _settingsService.CurrentSettings;
        StartWithWindows = settings.StartWithWindows;
        StartMinimized = settings.StartMinimized;
        ShowOverlayOnStartup = settings.ShowOverlayOnStartup;
        OverlayOpacity = settings.OverlayOpacity > 0 ? settings.OverlayOpacity : 0.9;
        OverlaySize = settings.OverlaySize > 0 ? settings.OverlaySize : 250;
        ClickThroughEnabled = settings.ClickThroughEnabled;
        ShowNotifications = settings.ShowNotifications;
        HotkeyPlayPause = settings.HotkeyPlayPause;
        HotkeyNext = settings.HotkeyNext;
        HotkeyPrevious = settings.HotkeyPrevious;
        HotkeyToggleOverlay = settings.HotkeyToggleOverlay;
        HotkeyToggleClickThrough = settings.HotkeyToggleClickThrough;
        DiscordEnabled = settings.DiscordEnabled;
        OBSWidgetEnabled = settings.OBSWidgetEnabled;
    }

    private void LoadThemes()
    {
        AvailableThemes = new ObservableCollection<ThemeInfo>(_themeService.GetAvailableThemes());
        var savedThemeId = _settingsService.CurrentSettings.SelectedTheme;
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Id == savedThemeId)
                        ?? AvailableThemes.FirstOrDefault();
        
        // Apply theme if it was saved (but don't save again during load)
        if (!string.IsNullOrEmpty(savedThemeId) && SelectedTheme != null)
        {
            _themeService.ApplyTheme(savedThemeId);
        }
    }

    private void LoadAnalytics()
    {
        RefreshAnalytics();
    }

    public void RefreshAnalytics()
    {
        System.Diagnostics.Debug.WriteLine("RefreshAnalytics called");
        var stats = _analyticsService.GetTodayStats();
        System.Diagnostics.Debug.WriteLine($"Stats: {stats.TotalListeningTime}, {stats.TracksPlayed}");
        TodayListeningTime = FormatDuration(stats.TotalListeningTime);
        TracksPlayedToday = stats.TracksPlayed;
        System.Diagnostics.Debug.WriteLine($"Updated: TodayListeningTime={TodayListeningTime}, TracksPlayedToday={TracksPlayedToday}");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{duration.Minutes}m {duration.Seconds}s";
    }

    partial void OnSelectedThemeChanged(ThemeInfo? value)
    {
        if (value != null && !_isLoadingSettings)
        {
            _themeService.ApplyTheme(value.Id);
            _settingsService.CurrentSettings.SelectedTheme = value.Id;
            _settingsService.SaveAsync();
        }
    }

    partial void OnOverlayOpacityChanged(double value)
    {
        if (!_isLoadingSettings)
        {
            _settingsService.CurrentSettings.OverlayOpacity = value;
            _overlayManager.UpdateOpacity(value);
            _settingsService.SaveAsync();
        }
    }

    partial void OnOverlaySizeChanged(double value)
    {
        if (!_isLoadingSettings)
        {
            _settingsService.CurrentSettings.OverlaySize = value;
            _overlayManager.UpdateSize(value);
            _settingsService.SaveAsync();
        }
    }

    partial void OnClickThroughEnabledChanged(bool value)
    {
        _settingsService.CurrentSettings.ClickThroughEnabled = value;
        _overlayManager.SetClickThrough(value);
        _settingsService.SaveAsync();
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        _settingsService.CurrentSettings.StartWithWindows = value;
        _settingsService.SaveAsync();
        // TODO: Implement Windows startup registry entry
    }

    partial void OnStartMinimizedChanged(bool value)
    {
        _settingsService.CurrentSettings.StartMinimized = value;
        _settingsService.SaveAsync();
    }

    partial void OnShowOverlayOnStartupChanged(bool value)
    {
        _settingsService.CurrentSettings.ShowOverlayOnStartup = value;
        _settingsService.SaveAsync();
    }

    partial void OnShowNotificationsChanged(bool value)
    {
        _settingsService.CurrentSettings.ShowNotifications = value;
        _settingsService.SaveAsync();
    }

    partial void OnHotkeyPlayPauseChanged(string value)
    {
        if (_isLoadingSettings) return;
        _settingsService.CurrentSettings.HotkeyPlayPause = value;
        _settingsService.SaveAsync();
        _hotkeyService.RegisterHotkey("PlayPause", value, async () => await _mediaService.PlayPauseAsync());
    }

    partial void OnHotkeyNextChanged(string value)
    {
        if (_isLoadingSettings) return;
        _settingsService.CurrentSettings.HotkeyNext = value;
        _settingsService.SaveAsync();
        _hotkeyService.RegisterHotkey("Next", value, async () => await _mediaService.NextAsync());
    }

    partial void OnHotkeyPreviousChanged(string value)
    {
        if (_isLoadingSettings) return;
        _settingsService.CurrentSettings.HotkeyPrevious = value;
        _settingsService.SaveAsync();
        _hotkeyService.RegisterHotkey("Previous", value, async () => await _mediaService.PreviousAsync());
    }

    partial void OnHotkeyToggleOverlayChanged(string value)
    {
        if (_isLoadingSettings) return;
        _settingsService.CurrentSettings.HotkeyToggleOverlay = value;
        _settingsService.SaveAsync();
        _hotkeyService.RegisterHotkey("ToggleOverlay", value, () => _overlayManager.Toggle());
    }

    partial void OnHotkeyToggleClickThroughChanged(string value)
    {
        if (_isLoadingSettings) return;
        _settingsService.CurrentSettings.HotkeyToggleClickThrough = value;
        _settingsService.SaveAsync();
        _hotkeyService.RegisterHotkey("ToggleClickThrough", value, () => _overlayManager.ToggleClickThrough());
    }

    partial void OnDiscordEnabledChanged(bool value)
    {
        if (_isLoadingSettings) return;
        
        _settingsService.CurrentSettings.DiscordEnabled = value;
        _settingsService.SaveAsync();
        
        // Reinitialize Discord service to apply the new setting
        if (value)
        {
            _discordService.Initialize();
        }
        else
        {
            _discordService.Dispose();
        }
    }

    partial void OnOBSWidgetEnabledChanged(bool value)
    {
        _settingsService.CurrentSettings.OBSWidgetEnabled = value;
        _settingsService.SaveAsync();
        // TODO: Enable/disable OBS widget server
    }

    [RelayCommand]
    private void ShowOverlay()
    {
        _overlayManager.Show();
    }

    [RelayCommand]
    private void HideToTray()
    {
        Application.Current.MainWindow?.Hide();
    }

    [RelayCommand]
    private void OpenAnalytics()
    {
        var analyticsWindow = new Views.AnalyticsWindow();
        analyticsWindow.Owner = Application.Current.MainWindow;
        analyticsWindow.Show();
    }

    [RelayCommand]
    private void ResetOverlayPosition()
    {
        _overlayManager.ResetPosition();
        _overlayManager.Show(); // Show the overlay so user can see where it is
    }
}
