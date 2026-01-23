using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public interface IMediaSessionService
{
    MediaInfo? CurrentMedia { get; }
    bool IsPlaying { get; }
    TimelineInfo? CurrentTimeline { get; }
    
    event EventHandler<MediaInfo>? MediaChanged;
    event EventHandler<bool>? PlaybackStateChanged;
    event EventHandler<TimelineInfo>? TimelineChanged;
    
    Task InitializeAsync();
    Task PlayPauseAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(TimeSpan position);
}
