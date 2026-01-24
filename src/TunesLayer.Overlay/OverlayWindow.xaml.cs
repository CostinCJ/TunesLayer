using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using TunesLayer.Core.Services;

namespace TunesLayer.Overlay;

public partial class OverlayWindow : Window
{
    #region Win32 Interop

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    #endregion

    private readonly IMediaSessionService _mediaService;
    private readonly ISettingsService _settingsService;
    private IntPtr _hwnd;
    private bool _isClickThrough;
    private bool _isDragging;
    private Point _dragStartPoint;

    public OverlayWindow(IMediaSessionService mediaService, ISettingsService settingsService)
    {
        InitializeComponent();
        _mediaService = mediaService;
        _settingsService = settingsService;

        // Setup data binding
        DataContext = new OverlayViewModel(mediaService);

        // Subscribe to playback state changes for icon updates
        _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        // Apply anti-cheat safe window styles
        ApplyAntiCheatSafeStyles();

        // Exclude from screen capture (makes it invisible to anti-cheat screen scanners)
        SetWindowDisplayAffinity(_hwnd, WDA_EXCLUDEFROMCAPTURE);

        // Initialize play/pause button with current state
        UpdatePlayPauseIcon(_mediaService.IsPlaying);

        // Fade in animation
        var fadeIn = (Storyboard)FindResource("FadeIn");
        BeginStoryboard(fadeIn);
    }

    private void UpdatePlayPauseIcon(bool isPlaying)
    {
        PlayPauseIcon.Text = isPlaying ? "\uE769" : "\uE768"; // Pause : Play
    }

    private void ApplyAntiCheatSafeStyles()
    {
        int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);

        // WS_EX_LAYERED - Required for transparency
        extendedStyle |= WS_EX_LAYERED;

        // WS_EX_TOOLWINDOW - Hides from Alt+Tab and taskbar
        extendedStyle |= WS_EX_TOOLWINDOW;

        // WS_EX_NOACTIVATE - Never steal focus from games
        extendedStyle |= WS_EX_NOACTIVATE;

        SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle);
    }

    public void SetClickThrough(bool enabled)
    {
        if (_hwnd == IntPtr.Zero) return;

        int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);

        if (enabled)
        {
            extendedStyle |= WS_EX_TRANSPARENT;
        }
        else
        {
            extendedStyle &= ~WS_EX_TRANSPARENT;
        }

        SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle);
        _isClickThrough = enabled;
    }

    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        Dispatcher.Invoke(() => UpdatePlayPauseIcon(isPlaying));
    }

    #region Control Event Handlers

    private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        await _mediaService.PlayPauseAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        await _mediaService.NextAsync();
    }

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        await _mediaService.PreviousAsync();
    }

    #endregion

    #region Drag Support

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isClickThrough) return;

        // Allow dragging the window
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            _isDragging = true;
            DragMove();
            _isDragging = false;

            // Save position after drag completes (DragMove blocks until mouse is released)
            _settingsService.CurrentSettings.OverlayX = Left;
            _settingsService.CurrentSettings.OverlayY = Top;
            _ = _settingsService.SaveAsync();
        }
    }

    #endregion

    #region Hover Effects

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);

        if (!_isClickThrough)
        {
            var fadeIn = (Storyboard)FindResource("ControlsFadeIn");
            BeginStoryboard(fadeIn);
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        var fadeOut = (Storyboard)FindResource("ControlsFadeOut");
        BeginStoryboard(fadeOut);
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _mediaService.PlaybackStateChanged -= OnPlaybackStateChanged;
        base.OnClosed(e);
    }
}

/// <summary>
/// Simple ViewModel for the overlay window
/// </summary>
public class OverlayViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private readonly IMediaSessionService _mediaService;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public OverlayViewModel(IMediaSessionService mediaService)
    {
        _mediaService = mediaService;
        _mediaService.MediaChanged += OnMediaChanged;
        _mediaService.TimelineChanged += OnTimelineChanged;

        // Load initial state
        if (_mediaService.CurrentMedia != null)
        {
            UpdateFromMedia(_mediaService.CurrentMedia);
        }
    }

    private string _trackTitle = "No media playing";
    public string TrackTitle
    {
        get => _trackTitle;
        set { _trackTitle = value; OnPropertyChanged(nameof(TrackTitle)); }
    }

    private string _artistName = "Open a music app";
    public string ArtistName
    {
        get => _artistName;
        set { _artistName = value; OnPropertyChanged(nameof(ArtistName)); }
    }

    private System.Windows.Media.Imaging.BitmapImage? _albumArt;
    public System.Windows.Media.Imaging.BitmapImage? AlbumArt
    {
        get => _albumArt;
        set { _albumArt = value; OnPropertyChanged(nameof(AlbumArt)); OnPropertyChanged(nameof(HasAlbumArt)); }
    }

    public bool HasAlbumArt => _albumArt != null;

    private string _sourceAppName = "";
    public string SourceAppName
    {
        get => _sourceAppName;
        set { _sourceAppName = value; OnPropertyChanged(nameof(SourceAppName)); }
    }

    private string _sourceAppIcon = "\uE8D6";
    public string SourceAppIcon
    {
        get => _sourceAppIcon;
        set { _sourceAppIcon = value; OnPropertyChanged(nameof(SourceAppIcon)); }
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(nameof(Progress)); }
    }

    private string _currentTime = "0:00";
    public string CurrentTime
    {
        get => _currentTime;
        set { _currentTime = value; OnPropertyChanged(nameof(CurrentTime)); }
    }

    private string _totalTime = "0:00";
    public string TotalTime
    {
        get => _totalTime;
        set { _totalTime = value; OnPropertyChanged(nameof(TotalTime)); }
    }

    private void OnMediaChanged(object? sender, TunesLayer.Core.Models.MediaInfo media)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() => UpdateFromMedia(media));
    }

    private void OnTimelineChanged(object? sender, TunesLayer.Core.Models.TimelineInfo timeline)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            if (timeline.Duration.TotalSeconds > 0)
            {
                Progress = timeline.Position.TotalSeconds / timeline.Duration.TotalSeconds * 100;
                CurrentTime = FormatTime(timeline.Position);
                TotalTime = FormatTime(timeline.Duration);
            }
        });
    }

    private void UpdateFromMedia(TunesLayer.Core.Models.MediaInfo media)
    {
        TrackTitle = media.Title ?? "Unknown Track";
        ArtistName = media.Artist ?? "Unknown Artist";
        SourceAppName = media.SourceApp ?? "";
        SourceAppIcon = GetSourceIcon(media.SourceApp);

        if (media.AlbumArtData != null)
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                using var stream = new System.IO.MemoryStream(media.AlbumArtData);
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                AlbumArt = bitmap;
            }
            catch
            {
                AlbumArt = null;
            }
        }
        else
        {
            AlbumArt = null;
        }
    }

    private static string GetSourceIcon(string? sourceApp)
    {
        return sourceApp?.ToLowerInvariant() switch
        {
            "spotify" => "\uE8D6",
            "apple music" => "\uE8D6",
            "youtube music" => "\uE714",
            _ => "\uE8D6"
        };
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
