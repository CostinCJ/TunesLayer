namespace TunesLayer.Core.Models;

public class ListeningSession
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public string? ActiveGame { get; set; }
}

public class TrackPlay
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string? TrackTitle { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? SourceApp { get; set; }
    public DateTime PlayedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ActiveGame { get; set; }
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public TimeSpan TotalListeningTime { get; set; }
    public int TracksPlayed { get; set; }
    public int SessionsCount { get; set; }
    public Dictionary<string, int> TopArtists { get; set; } = new();
    public Dictionary<string, TimeSpan> GameMusicCorrelation { get; set; } = new();
}
