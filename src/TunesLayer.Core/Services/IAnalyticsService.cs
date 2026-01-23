using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public interface IAnalyticsService
{
    void StartSession();
    void EndSession();
    void TrackMediaPlayed(MediaInfo media);
    DailyStats GetTodayStats();
    IEnumerable<DailyStats> GetWeeklyStats();
    IEnumerable<TrackPlay> GetRecentTracks(int count = 50);
    Task ExportDataAsync(string filePath);
}
