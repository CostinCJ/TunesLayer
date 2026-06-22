using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TunesLayer.Core.Services;
using TunesLayer.Overlay;
using TunesLayer.Integrations.Discord;
using TunesLayer.Integrations.OBS;

namespace TunesLayer.App;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private static System.Windows.Forms.NotifyIcon? _staticNotifyIcon;

    public static IServiceProvider Services { get; private set; } = null!;

    public static void ShowBalloonTip(string title, string text, int timeoutMs = 2000)
    {
        if (_staticNotifyIcon != null)
        {
            _staticNotifyIcon.ShowBalloonTip(timeoutMs, title, text, System.Windows.Forms.ToolTipIcon.Info);
        }
    }

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IMediaSessionService, MediaSessionService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IVolumeService, VolumeService>();
        services.AddSingleton<IStartupManager, StartupManager>();

        // Overlay
        services.AddSingleton<IOverlayManager, OverlayManager>();

        // Integrations
        services.AddSingleton<IDiscordRpcService, DiscordRpcService>();
        services.AddSingleton<IOBSWidgetService, OBSWidgetService>();

        // ViewModels
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.OverlayViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        // Apply saved theme on startup
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var savedTheme = settingsService.CurrentSettings.SelectedTheme;
        if (!string.IsNullOrEmpty(savedTheme))
        {
            themeService.ApplyTheme(savedTheme);
        }

        var mediaService = _serviceProvider.GetRequiredService<IMediaSessionService>();
        await mediaService.InitializeAsync();

        var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        hotkeyService.Initialize();
        
        // Register hotkeys from settings
        var settings = settingsService.CurrentSettings;
        hotkeyService.RegisterHotkey("PlayPause", settings.HotkeyPlayPause, async () => await mediaService.PlayPauseAsync());
        hotkeyService.RegisterHotkey("Next", settings.HotkeyNext, async () => await mediaService.NextAsync());
        hotkeyService.RegisterHotkey("Previous", settings.HotkeyPrevious, async () => await mediaService.PreviousAsync());
        
        // Volume control hotkeys
        var volumeService = _serviceProvider.GetRequiredService<IVolumeService>();
        hotkeyService.RegisterHotkey("VolumeUp", settings.HotkeyVolumeUp, () => volumeService.IncreaseVolume());
        hotkeyService.RegisterHotkey("VolumeDown", settings.HotkeyVolumeDown, () => volumeService.DecreaseVolume());
        
        // Initialize overlay first so we can register hotkey for it
        var overlayManager = _serviceProvider.GetRequiredService<IOverlayManager>();
        if (overlayManager is Overlay.OverlayManager om)
        {
            om.ShowNotificationCallback = (title, text) => ShowBalloonTip(title, text, 3000);
        }
        overlayManager.Initialize();
        hotkeyService.RegisterHotkey("ToggleOverlay", settings.HotkeyToggleOverlay, () => overlayManager.Toggle());
        hotkeyService.RegisterHotkey("ToggleClickThrough", settings.HotkeyToggleClickThrough, () => overlayManager.ToggleClickThrough());

        // Start analytics tracking
        var analyticsService = _serviceProvider.GetRequiredService<IAnalyticsService>();
        analyticsService.StartSession();

        // Initialize Discord integration
        var discordService = _serviceProvider.GetRequiredService<IDiscordRpcService>();
        discordService.Initialize();

        // Setup system tray
        SetupSystemTray();

        // Create and show main window after settings are loaded
        // This ensures the ViewModel can access loaded settings
        var mainWindow = new Views.MainWindow();
        Current.MainWindow = mainWindow;
        
        // Show window based on startup settings
        if (!settingsService.CurrentSettings.StartMinimized)
        {
            mainWindow.Show();
        }
    }

    private void SetupSystemTray()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(
                Application.GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"))?.Stream 
                ?? throw new InvalidOperationException("Icon not found")),
            Visible = true,
            Text = "TunesLayer"
        };
        _staticNotifyIcon = _notifyIcon;

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show Overlay", null, (s, e) => ShowOverlay());
        contextMenu.Items.Add("Hide Overlay", null, (s, e) => HideOverlay());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ToggleOverlay();
    }

    private void ShowOverlay()
    {
        var overlayManager = _serviceProvider.GetRequiredService<IOverlayManager>();
        overlayManager.Show();
    }

    private void HideOverlay()
    {
        var overlayManager = _serviceProvider.GetRequiredService<IOverlayManager>();
        overlayManager.Hide();
    }

    private void ToggleOverlay()
    {
        var overlayManager = _serviceProvider.GetRequiredService<IOverlayManager>();
        overlayManager.Toggle();
    }

    private void ShowSettings()
    {
        var mainWindow = Current.MainWindow;
        if (mainWindow == null)
        {
            mainWindow = new Views.MainWindow();
            Current.MainWindow = mainWindow;
        }
        mainWindow.Show();
        mainWindow.Activate();
    }

    private void ExitApplication()
    {
        var analyticsService = _serviceProvider.GetRequiredService<IAnalyticsService>();
        analyticsService.EndSession();

        var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        hotkeyService.Dispose();

        _notifyIcon?.Dispose();
        _serviceProvider.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
