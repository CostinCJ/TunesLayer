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

            // Update timeline to get current duration before creating MediaInfo
            UpdateTimelineBaseline();

            CurrentMedia = new MediaInfo
            {
                Title = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                Album = mediaProperties.AlbumTitle,
                AlbumArtData = albumArtData,
                SourceApp = GetSourceAppName(mediaProperties),
                TrackId = $"{mediaProperties.Artist}_{mediaProperties.Title}",
                Duration = _duration
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

    private string GetSourceAppName(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties = null)
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
            
            // Browser detection - try to identify the specific service
            if (sourceAppId.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
                sourceAppId.Contains("Firefox", StringComparison.OrdinalIgnoreCase) ||
                sourceAppId.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            {
                // Try to identify YouTube or YouTube Music from media properties
                if (mediaProperties != null)
                {
                    var title = mediaProperties.Title ?? "";
                    var artist = mediaProperties.Artist ?? "";
                    var album = mediaProperties.AlbumTitle ?? "";
                    
                    // Priority 1: Check for explicit "YouTube Music" indicators
                    if (title.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase) ||
                        artist.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase) ||
                        album.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase))
                    {
                        return "YouTube Music";
                    }
                    
                    // Priority 2: Check for music.youtube.com domain indicators
                    // Some browsers may include domain info in metadata
                    if (title.Contains("music.youtube", StringComparison.OrdinalIgnoreCase) ||
                        artist.Contains("music.youtube", StringComparison.OrdinalIgnoreCase))
                    {
                        return "YouTube Music";
                    }
                    
                    // Priority 3: Check for regular YouTube indicators (but not YouTube Music)
                    bool hasYouTubeIndicator = title.Contains("YouTube", StringComparison.OrdinalIgnoreCase) ||
                                               artist.Contains("YouTube", StringComparison.OrdinalIgnoreCase);
                    
                    if (hasYouTubeIndicator)
                    {
                        // If it says "YouTube" but not "YouTube Music", it's regular YouTube
                        return "YouTube";
                    }
                    
                    // Priority 4: Heuristic-based detection
                    // YouTube Music characteristics:
                    // - Has artist metadata that's different from title
                    // - Often (but not always) has album metadata
                    // - Artist typically doesn't have channel-like patterns
                    
                    bool hasDistinctArtist = !string.IsNullOrWhiteSpace(artist) && artist != title;
                    bool hasAlbum = !string.IsNullOrWhiteSpace(album);
                    
                    // Check for patterns that suggest it's a YouTube channel rather than music metadata
                    bool artistLooksLikeChannel = artist.Contains("VEVO", StringComparison.OrdinalIgnoreCase) ||
                                                   artist.Contains(" - Topic", StringComparison.OrdinalIgnoreCase) ||
                                                   artist.EndsWith(" - Topic", StringComparison.OrdinalIgnoreCase);
                    
                    // Check for patterns in title that suggest regular YouTube video
                    bool titleLooksLikeVideo = title.Contains("|") ||
                                               title.Contains("(Official Video)") ||
                                               title.Contains("(Official Music Video)") ||
                                               title.Contains("[Official") ||
                                               title.Length > 100;
                    
                    // YouTube Music detection: has distinct artist AND (has album OR clean title)
                    if (hasDistinctArtist && !artistLooksLikeChannel)
                    {
                        // If we have album info, it's very likely YouTube Music
                        if (hasAlbum)
                        {
                            return "YouTube Music";
                        }
                        
                        // Even without album, if title is clean and not video-like, likely YouTube Music
                        if (!titleLooksLikeVideo && title.Length < 80)
                        {
                            return "YouTube Music";
                        }
                    }
                    
                    // If we have any media metadata but it doesn't match YouTube Music patterns,
                    // assume it's regular YouTube
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return "YouTube";
                    }
                }
                
                return "Browser";
            }

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
