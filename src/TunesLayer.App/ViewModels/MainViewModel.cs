using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TunesLayer.Core.Models;
using TunesLayer.Core.Services;
using TunesLayer.Overlay;

namespace TunesLayer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IOverlayManager _overlayManager;
    private readonly IThemeService _themeService;
    private readonly IAnalyticsService _analyticsService;

    public MainViewModel()
    {
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _overlayManager = App.Services.GetRequiredService<IOverlayManager>();
        _themeService = App.Services.GetRequiredService<IThemeService>();
        _analyticsService = App.Services.GetRequiredService<IAnalyticsService>();

        LoadSettings();
        LoadThemes();
        LoadAnalytics();
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
        OverlayOpacity = settings.OverlayOpacity;
        OverlaySize = settings.OverlaySize;
        ClickThroughEnabled = settings.ClickThroughEnabled;
        ShowNotifications = settings.ShowNotifications;
        DiscordEnabled = settings.DiscordEnabled;
        OBSWidgetEnabled = settings.OBSWidgetEnabled;
    }

    private void LoadThemes()
    {
        AvailableThemes = new ObservableCollection<ThemeInfo>(_themeService.GetAvailableThemes());
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Id == _settingsService.CurrentSettings.SelectedTheme)
                        ?? AvailableThemes.FirstOrDefault();
    }

    private void LoadAnalytics()
    {
        var stats = _analyticsService.GetTodayStats();
        TodayListeningTime = FormatDuration(stats.TotalListeningTime);
        TracksPlayedToday = stats.TracksPlayed;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{duration.Minutes}m {duration.Seconds}s";
    }

    partial void OnSelectedThemeChanged(ThemeInfo? value)
    {
        if (value != null)
        {
            _themeService.ApplyTheme(value.Id);
            _settingsService.CurrentSettings.SelectedTheme = value.Id;
            _settingsService.SaveAsync();
        }
    }

    partial void OnOverlayOpacityChanged(double value)
    {
        _settingsService.CurrentSettings.OverlayOpacity = value;
        _overlayManager.UpdateOpacity(value);
        _settingsService.SaveAsync();
    }

    partial void OnOverlaySizeChanged(double value)
    {
        _settingsService.CurrentSettings.OverlaySize = value;
        _overlayManager.UpdateSize(value);
        _settingsService.SaveAsync();
    }

    partial void OnClickThroughEnabledChanged(bool value)
    {
        _settingsService.CurrentSettings.ClickThroughEnabled = value;
        _overlayManager.SetClickThrough(value);
        _settingsService.SaveAsync();
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
        // Open analytics window (to be implemented)
    }
}
