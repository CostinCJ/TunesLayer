namespace TunesLayer.Core.Models;

public class MediaInfo
{
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public byte[]? AlbumArtData { get; set; }
    public string? SourceApp { get; set; }
    public string? TrackId { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class TimelineInfo
{
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
