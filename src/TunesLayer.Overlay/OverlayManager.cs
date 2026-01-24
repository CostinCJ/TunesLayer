using System.Windows;
using TunesLayer.Core.Services;

namespace TunesLayer.Overlay;

public class OverlayManager : IOverlayManager
{
    private readonly ISettingsService _settingsService;
    private readonly IMediaSessionService _mediaService;
    private readonly IAnalyticsService _analyticsService;
    private OverlayWindow? _overlayWindow;
    
    public Action<string, string>? ShowNotificationCallback { get; set; }

    public bool IsVisible => _overlayWindow?.IsVisible ?? false;

    public OverlayManager(ISettingsService settingsService, IMediaSessionService mediaService, IAnalyticsService analyticsService)
    {
        _settingsService = settingsService;
        _mediaService = mediaService;
        _analyticsService = analyticsService;
    }

    public void Initialize()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow = new OverlayWindow(_mediaService, _settingsService, _analyticsService, ShowNotificationCallback);
            
            // Apply saved settings
            // Note: opacity and click-through are applied in Window_Loaded after the hwnd is available
            var settings = _settingsService.CurrentSettings;
            _overlayWindow.Width = settings.OverlaySize;
            _overlayWindow.Height = settings.OverlaySize;
            _overlayWindow.Left = settings.OverlayX;
            _overlayWindow.Top = settings.OverlayY;

            if (settings.ShowOverlayOnStartup)
            {
                _overlayWindow.Show();
            }
        });
    }

    public void Show()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow?.Show();
        });
    }

    public void Hide()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow?.Hide();
        });
    }

    public void Toggle()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlayWindow == null) return;

            if (_overlayWindow.IsVisible)
                _overlayWindow.Hide();
            else
                _overlayWindow.Show();
        });
    }

    public void UpdateOpacity(double opacity)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlayWindow != null)
                _overlayWindow.SetContentOpacity(opacity);
        });
    }

    public void UpdateSize(double size)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Width = size;
                _overlayWindow.Height = size;
            }
        });
    }

    public void SetClickThrough(bool enabled)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow?.SetClickThrough(enabled);
        });
    }

    public bool IsClickThrough => _overlayWindow?.IsClickThrough ?? false;

    public void ToggleClickThrough()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlayWindow == null) return;
            
            bool newState = !_overlayWindow.IsClickThrough;
            _overlayWindow.SetClickThrough(newState);
            
            // Save the setting
            _settingsService.CurrentSettings.ClickThroughEnabled = newState;
            _ = _settingsService.SaveAsync();
        });
    }

    public void SetPosition(double x, double y)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Left = x;
                _overlayWindow.Top = y;
            }
        });
    }

    public void ResetPosition()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_overlayWindow != null)
            {
                // Center on screen
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                _overlayWindow.Left = (screenWidth - _overlayWindow.Width) / 2;
                _overlayWindow.Top = (screenHeight - _overlayWindow.Height) / 2;
                
                // Save position
                _settingsService.CurrentSettings.OverlayX = _overlayWindow.Left;
                _settingsService.CurrentSettings.OverlayY = _overlayWindow.Top;
                _ = _settingsService.SaveAsync();
            }
        });
    }
}
