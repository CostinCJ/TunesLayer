using System.Windows;
using TunesLayer.Core.Services;

namespace TunesLayer.Overlay;

public class OverlayManager : IOverlayManager
{
    private readonly ISettingsService _settingsService;
    private readonly IMediaSessionService _mediaService;
    private OverlayWindow? _overlayWindow;

    public bool IsVisible => _overlayWindow?.IsVisible ?? false;

    public OverlayManager(ISettingsService settingsService, IMediaSessionService mediaService)
    {
        _settingsService = settingsService;
        _mediaService = mediaService;
    }

    public void Initialize()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _overlayWindow = new OverlayWindow(_mediaService, _settingsService);
            
            // Apply saved settings
            var settings = _settingsService.CurrentSettings;
            _overlayWindow.Opacity = settings.OverlayOpacity;
            _overlayWindow.Width = settings.OverlaySize;
            _overlayWindow.Height = settings.OverlaySize;
            _overlayWindow.Left = settings.OverlayX;
            _overlayWindow.Top = settings.OverlayY;

            if (settings.ClickThroughEnabled)
            {
                _overlayWindow.SetClickThrough(true);
            }

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
                _overlayWindow.Opacity = opacity;
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
}
