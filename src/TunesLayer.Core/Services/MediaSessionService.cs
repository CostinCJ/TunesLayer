using System.Runtime.InteropServices;
using TunesLayer.Core.Models;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace TunesLayer.Core.Services;

public class MediaSessionService : IMediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private System.Timers.Timer? _timelineTimer;
    
    // For real-time position interpolation
    private TimeSpan _smtcPosition;      // Position reported by SMTC
    private DateTimeOffset _smtcUpdateTime; // When SMTC reported this position
    private TimeSpan _duration;

    public MediaInfo? CurrentMedia { get; private set; }
    public bool IsPlaying { get; private set; }
    public TimelineInfo? CurrentTimeline { get; private set; }

    public event EventHandler<MediaInfo>? MediaChanged;
    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<TimelineInfo>? TimelineChanged;

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
            _sessionManager.SessionsChanged += OnSessionsChanged;

            // Get the current session
            UpdateCurrentSession();

            // Start timeline update timer
            _timelineTimer = new System.Timers.Timer(1000);
            _timelineTimer.Elapsed += (s, e) => UpdateTimeline();
            _timelineTimer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize media session: {ex.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession();
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        UpdateCurrentSession();
    }

    private void UpdateCurrentSession()
    {
        // Unsubscribe from old session
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }

        // Get new current session
        _currentSession = _sessionManager?.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

            // Load initial state
            _ = LoadMediaPropertiesAsync();
            LoadPlaybackInfo();
            UpdateTimelineBaseline();
        }
        else
        {
            CurrentMedia = null;
            IsPlaying = false;
        }
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await LoadMediaPropertiesAsync();
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        LoadPlaybackInfo();
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        // Update the baseline from SMTC (this fires on seek, track change, etc.)
        UpdateTimelineBaseline();
    }

    private async Task LoadMediaPropertiesAsync()
    {
        if (_currentSession == null) return;

        try
        {
            var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();
            if (mediaProperties == null) return;

            byte[]? albumArtData = null;
            if (mediaProperties.Thumbnail != null)
            {
                try
                {
                    using var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                    using var reader = new DataReader(stream);
                    var bytes = new byte[stream.Size];
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                    albumArtData = bytes;
                }
                catch
                {
                    // Album art not available
                }
            }

            CurrentMedia = new MediaInfo
            {
                Title = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                Album = mediaProperties.AlbumTitle,
                AlbumArtData = albumArtData,
                SourceApp = GetSourceAppName(),
                TrackId = $"{mediaProperties.Artist}_{mediaProperties.Title}"
            };

            MediaChanged?.Invoke(this, CurrentMedia);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load media properties: {ex.Message}");
        }
    }

    private void LoadPlaybackInfo()
    {
        if (_currentSession == null) return;

        try
        {
            var playbackInfo = _currentSession.GetPlaybackInfo();
            var wasPlaying = IsPlaying;
            IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            if (wasPlaying != IsPlaying)
            {
                // Get fresh baseline on play/pause state change
                UpdateTimelineBaseline();
                
                PlaybackStateChanged?.Invoke(this, IsPlaying);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load playback info: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the baseline position from SMTC. Called on seek, track change, play/pause.
    /// </summary>
    private void UpdateTimelineBaseline()
    {
        if (_currentSession == null) return;

        try
        {
            var timeline = _currentSession.GetTimelineProperties();
            _duration = timeline.EndTime - timeline.StartTime;
            _smtcPosition = timeline.Position;
            _smtcUpdateTime = timeline.LastUpdatedTime;
            
            // Immediately broadcast the updated position
            BroadcastCurrentPosition();
        }
        catch
        {
            // Timeline not available
        }
    }

    /// <summary>
    /// Called by timer every second to interpolate and broadcast current position.
    /// </summary>
    private void UpdateTimeline()
    {
        if (_currentSession == null) return;
        
        // Just broadcast the interpolated position - don't query SMTC
        BroadcastCurrentPosition();
    }

    /// <summary>
    /// Calculates interpolated position and broadcasts it.
    /// </summary>
    private void BroadcastCurrentPosition()
    {
        try
        {
            TimeSpan currentPosition;
            
            if (IsPlaying && _smtcUpdateTime != default)
            {
                // Calculate elapsed time since SMTC last reported position
                var elapsed = DateTimeOffset.Now - _smtcUpdateTime;
                currentPosition = _smtcPosition + elapsed;
                
                // Clamp to duration
                if (currentPosition > _duration)
                    currentPosition = _duration;
                if (currentPosition < TimeSpan.Zero)
                    currentPosition = TimeSpan.Zero;
            }
            else
            {
                currentPosition = _smtcPosition;
            }
            
            CurrentTimeline = new TimelineInfo
            {
                Position = currentPosition,
                Duration = _duration
            };

            TimelineChanged?.Invoke(this, CurrentTimeline);
        }
        catch
        {
            // Timeline not available
        }
    }

    private string GetSourceAppName()
    {
        if (_currentSession == null) return "Unknown";

        try
        {
            var sourceAppId = _currentSession.SourceAppUserModelId;
            
            // Common app IDs
            if (sourceAppId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                return "Spotify";
            if (sourceAppId.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase) || 
                sourceAppId.Contains("Apple.Music", StringComparison.OrdinalIgnoreCase))
                return "Apple Music";
            if (sourceAppId.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
                return "YouTube Music";
            if (sourceAppId.Contains("Amazon", StringComparison.OrdinalIgnoreCase))
                return "Amazon Music";
            if (sourceAppId.Contains("Tidal", StringComparison.OrdinalIgnoreCase))
                return "Tidal";
            if (sourceAppId.Contains("Deezer", StringComparison.OrdinalIgnoreCase))
                return "Deezer";
            if (sourceAppId.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
                sourceAppId.Contains("Firefox", StringComparison.OrdinalIgnoreCase) ||
                sourceAppId.Contains("Edge", StringComparison.OrdinalIgnoreCase))
                return "Browser";

            return sourceAppId;
        }
        catch
        {
            return "Unknown";
        }
    }

    public async Task PlayPauseAsync()
    {
        if (_currentSession == null) return;

        try
        {
            if (IsPlaying)
                await _currentSession.TryPauseAsync();
            else
                await _currentSession.TryPlayAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlayPause failed: {ex.Message}");
        }
    }

    public async Task NextAsync()
    {
        if (_currentSession == null) return;

        try
        {
            await _currentSession.TrySkipNextAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Next failed: {ex.Message}");
        }
    }

    public async Task PreviousAsync()
    {
        if (_currentSession == null) return;

        try
        {
            await _currentSession.TrySkipPreviousAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Previous failed: {ex.Message}");
        }
    }

    public async Task SeekAsync(TimeSpan position)
    {
        if (_currentSession == null) return;

        try
        {
            await _currentSession.TryChangePlaybackPositionAsync(position.Ticks);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Seek failed: {ex.Message}");
        }
    }
}
