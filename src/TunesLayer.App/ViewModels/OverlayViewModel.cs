using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TunesLayer.Core.Models;
using TunesLayer.Core.Services;

namespace TunesLayer.App.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    private readonly IMediaSessionService _mediaService;
    private readonly IAnalyticsService _analyticsService;

    public OverlayViewModel()
    {
        _mediaService = App.Services.GetRequiredService<IMediaSessionService>();
        _analyticsService = App.Services.GetRequiredService<IAnalyticsService>();

        _mediaService.MediaChanged += OnMediaChanged;
        _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;
        _mediaService.TimelineChanged += OnTimelineChanged;

        // Load initial state
        UpdateFromCurrentMedia();
    }

    [ObservableProperty]
    private string _trackTitle = "No media playing";

    [ObservableProperty]
    private string _artistName = "Open a music app to get started";

    [ObservableProperty]
    private string _albumName = string.Empty;

    [ObservableProperty]
    private BitmapImage? _albumArt;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _currentTime = "0:00";

    [ObservableProperty]
    private string _totalTime = "0:00";

    [ObservableProperty]
    private string _sourceAppName = string.Empty;

    [ObservableProperty]
    private string _sourceAppIcon = string.Empty;

    [ObservableProperty]
    private bool _hasMedia;

    private void OnMediaChanged(object? sender, MediaInfo media)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            TrackTitle = media.Title ?? "Unknown Track";
            ArtistName = media.Artist ?? "Unknown Artist";
            AlbumName = media.Album ?? string.Empty;
            SourceAppName = media.SourceApp ?? string.Empty;
            SourceAppIcon = GetSourceAppIcon(media.SourceApp);
            HasMedia = true;

            if (media.AlbumArtData != null)
            {
                var bitmap = new BitmapImage();
                using var stream = new System.IO.MemoryStream(media.AlbumArtData);
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                AlbumArt = bitmap;
            }
            else
            {
                AlbumArt = null;
            }

            // Track for analytics
            _analyticsService.TrackMediaPlayed(media);
        });
    }

    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsPlaying = isPlaying;
        });
    }

    private void OnTimelineChanged(object? sender, TimelineInfo timeline)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (timeline.Duration.TotalSeconds > 0)
            {
                Progress = timeline.Position.TotalSeconds / timeline.Duration.TotalSeconds * 100;
                CurrentTime = FormatTime(timeline.Position);
                TotalTime = FormatTime(timeline.Duration);
            }
        });
    }

    private void UpdateFromCurrentMedia()
    {
        var media = _mediaService.CurrentMedia;
        if (media != null)
        {
            OnMediaChanged(this, media);
            IsPlaying = _mediaService.IsPlaying;
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
    }

    private static string GetSourceAppIcon(string? sourceApp)
    {
        return sourceApp?.ToLowerInvariant() switch
        {
            "spotify" or "spotify.exe" => "\uE8D6", // Music note icon
            "apple music" or "applemusic" => "\uE8D6",
            "youtube music" or "youtube" => "\uE714", // Video icon
            "tidal" => "\uE8D6",
            "amazon music" => "\uE8D6",
            "deezer" => "\uE8D6",
            _ => "\uE8D6" // Default music icon
        };
    }

    [RelayCommand]
    private async Task PlayPause()
    {
        await _mediaService.PlayPauseAsync();
    }

    [RelayCommand]
    private async Task NextTrack()
    {
        await _mediaService.NextAsync();
    }

    [RelayCommand]
    private async Task PreviousTrack()
    {
        await _mediaService.PreviousAsync();
    }
}
