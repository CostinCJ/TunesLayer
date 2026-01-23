using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public class AnalyticsService : IAnalyticsService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TunesLayer");
    
    private static readonly string DatabasePath = Path.Combine(DataDirectory, "analytics.db");
    
    private readonly string _connectionString;
    private int _currentSessionId;
    private string? _lastTrackId;
    private DateTime _lastTrackTime;
    private System.Timers.Timer? _activeWindowTimer;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    public AnalyticsService()
    {
        Directory.CreateDirectory(DataDirectory);
        _connectionString = $"Data Source={DatabasePath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                start_time TEXT NOT NULL,
                end_time TEXT,
                active_game TEXT
            );

            CREATE TABLE IF NOT EXISTS track_plays (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id INTEGER,
                track_title TEXT,
                artist TEXT,
                album TEXT,
                source_app TEXT,
                played_at TEXT NOT NULL,
                duration_seconds INTEGER,
                active_game TEXT,
                FOREIGN KEY (session_id) REFERENCES sessions(id)
            );

            CREATE INDEX IF NOT EXISTS idx_track_plays_date ON track_plays(played_at);
            CREATE INDEX IF NOT EXISTS idx_sessions_date ON sessions(start_time);
        ";
        command.ExecuteNonQuery();
    }

    public void StartSession()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO sessions (start_time, active_game)
            VALUES (@startTime, @activeGame);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("@startTime", DateTime.Now.ToString("o"));
        command.Parameters.AddWithValue("@activeGame", GetActiveWindowTitle());

        _currentSessionId = Convert.ToInt32(command.ExecuteScalar());

        // Start monitoring active window
        _activeWindowTimer = new System.Timers.Timer(5000);
        _activeWindowTimer.Elapsed += (s, e) => UpdateSessionGame();
        _activeWindowTimer.Start();
    }

    public void EndSession()
    {
        _activeWindowTimer?.Stop();
        _activeWindowTimer?.Dispose();

        if (_currentSessionId <= 0) return;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE sessions 
            SET end_time = @endTime 
            WHERE id = @sessionId
        ";
        command.Parameters.AddWithValue("@endTime", DateTime.Now.ToString("o"));
        command.Parameters.AddWithValue("@sessionId", _currentSessionId);
        command.ExecuteNonQuery();
    }

    private void UpdateSessionGame()
    {
        var activeWindow = GetActiveWindowTitle();
        if (string.IsNullOrEmpty(activeWindow)) return;

        // Check if it looks like a game (simple heuristic)
        if (IsLikelyGame(activeWindow))
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE sessions 
                SET active_game = @activeGame 
                WHERE id = @sessionId
            ";
            command.Parameters.AddWithValue("@activeGame", activeWindow);
            command.Parameters.AddWithValue("@sessionId", _currentSessionId);
            command.ExecuteNonQuery();
        }
    }

    private static bool IsLikelyGame(string windowTitle)
    {
        // Common game window indicators
        var gameIndicators = new[]
        {
            "Valorant", "League of Legends", "Counter-Strike", "CS2", "CSGO",
            "Minecraft", "Fortnite", "Apex Legends", "Overwatch", "Dota",
            "Call of Duty", "Battlefield", "GTA", "FIFA", "NBA", "Rocket League",
            "Elden Ring", "Dark Souls", "Cyberpunk", "Witcher", "Steam",
            "Epic Games", "Battle.net", "Origin"
        };

        return gameIndicators.Any(g => 
            windowTitle.Contains(g, StringComparison.OrdinalIgnoreCase));
    }

    public void TrackMediaPlayed(MediaInfo media)
    {
        if (media == null || string.IsNullOrEmpty(media.Title)) return;

        var trackId = $"{media.Artist}_{media.Title}";
        
        // Avoid duplicate entries for the same track within 5 seconds
        if (trackId == _lastTrackId && (DateTime.Now - _lastTrackTime).TotalSeconds < 5)
            return;

        _lastTrackId = trackId;
        _lastTrackTime = DateTime.Now;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO track_plays (session_id, track_title, artist, album, source_app, played_at, duration_seconds, active_game)
            VALUES (@sessionId, @title, @artist, @album, @sourceApp, @playedAt, @duration, @activeGame)
        ";
        command.Parameters.AddWithValue("@sessionId", _currentSessionId > 0 ? _currentSessionId : DBNull.Value);
        command.Parameters.AddWithValue("@title", media.Title ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@artist", media.Artist ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@album", media.Album ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@sourceApp", media.SourceApp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@playedAt", DateTime.Now.ToString("o"));
        command.Parameters.AddWithValue("@duration", (int)media.Duration.TotalSeconds);
        command.Parameters.AddWithValue("@activeGame", GetActiveWindowTitle() ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    public DailyStats GetTodayStats()
    {
        var today = DateTime.Today;
        return GetDailyStats(today);
    }

    public IEnumerable<DailyStats> GetWeeklyStats()
    {
        var stats = new List<DailyStats>();
        for (int i = 6; i >= 0; i--)
        {
            stats.Add(GetDailyStats(DateTime.Today.AddDays(-i)));
        }
        return stats;
    }

    private DailyStats GetDailyStats(DateTime date)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var stats = new DailyStats { Date = date };

        // Get track count and artists
        var trackCommand = connection.CreateCommand();
        trackCommand.CommandText = @"
            SELECT COUNT(*), artist, SUM(duration_seconds)
            FROM track_plays 
            WHERE date(played_at) = date(@date)
            GROUP BY artist
            ORDER BY COUNT(*) DESC
        ";
        trackCommand.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

        using var reader = trackCommand.ExecuteReader();
        int totalTracks = 0;
        int totalSeconds = 0;
        
        while (reader.Read())
        {
            var count = reader.GetInt32(0);
            var artist = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
            var seconds = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            
            totalTracks += count;
            totalSeconds += seconds;
            
            if (!string.IsNullOrEmpty(artist))
            {
                stats.TopArtists[artist] = count;
            }
        }

        stats.TracksPlayed = totalTracks;
        stats.TotalListeningTime = TimeSpan.FromSeconds(totalSeconds);

        // Get session count
        var sessionCommand = connection.CreateCommand();
        sessionCommand.CommandText = @"
            SELECT COUNT(*) FROM sessions 
            WHERE date(start_time) = date(@date)
        ";
        sessionCommand.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
        stats.SessionsCount = Convert.ToInt32(sessionCommand.ExecuteScalar());

        return stats;
    }

    public IEnumerable<TrackPlay> GetRecentTracks(int count = 50)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, session_id, track_title, artist, album, source_app, played_at, duration_seconds, active_game
            FROM track_plays 
            ORDER BY played_at DESC 
            LIMIT @count
        ";
        command.Parameters.AddWithValue("@count", count);

        var tracks = new List<TrackPlay>();
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            tracks.Add(new TrackPlay
            {
                Id = reader.GetInt32(0),
                SessionId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                TrackTitle = reader.IsDBNull(2) ? null : reader.GetString(2),
                Artist = reader.IsDBNull(3) ? null : reader.GetString(3),
                Album = reader.IsDBNull(4) ? null : reader.GetString(4),
                SourceApp = reader.IsDBNull(5) ? null : reader.GetString(5),
                PlayedAt = DateTime.Parse(reader.GetString(6)),
                Duration = TimeSpan.FromSeconds(reader.IsDBNull(7) ? 0 : reader.GetInt32(7)),
                ActiveGame = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return tracks;
    }

    public async Task ExportDataAsync(string filePath)
    {
        var data = new
        {
            ExportDate = DateTime.Now,
            WeeklyStats = GetWeeklyStats(),
            RecentTracks = GetRecentTracks(500)
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private static string? GetActiveWindowTitle()
    {
        const int maxLength = 256;
        var buffer = new System.Text.StringBuilder(maxLength);
        var handle = GetForegroundWindow();
        
        if (GetWindowText(handle, buffer, maxLength) > 0)
        {
            return buffer.ToString();
        }
        
        return null;
    }
}
