using DiscordRPC;
using DiscordRPC.Logging;
using TunesLayer.Core.Models;
using TunesLayer.Core.Services;

namespace TunesLayer.Integrations.Discord;

public class DiscordRpcService : IDiscordRpcService, IDisposable
{
    // Note: You'll need to create a Discord application and get an App ID
    // https://discord.com/developers/applications
    private const string DISCORD_APP_ID = "YOUR_DISCORD_APP_ID";
    
    private DiscordRpcClient? _client;
    private readonly IMediaSessionService _mediaService;
    private readonly ISettingsService _settingsService;
    private bool _disposed;

    public bool IsConnected => _client?.IsInitialized ?? false;

    public DiscordRpcService(IMediaSessionService mediaService, ISettingsService settingsService)
    {
        _mediaService = mediaService;
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        if (!_settingsService.CurrentSettings.DiscordEnabled)
            return;

        try
        {
            _client = new DiscordRpcClient(DISCORD_APP_ID)
            {
                Logger = new ConsoleLogger() { Level = LogLevel.Warning }
            };

            _client.OnReady += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Discord RPC connected: {e.User.Username}");
            };

            _client.OnError += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Discord RPC error: {e.Message}");
            };

            _client.Initialize();

            // Subscribe to media changes
            _mediaService.MediaChanged += OnMediaChanged;
            _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;

            // Update with current media if available
            if (_mediaService.CurrentMedia != null && _mediaService.IsPlaying)
            {
                UpdatePresenceFromMedia(_mediaService.CurrentMedia);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize Discord RPC: {ex.Message}");
        }
    }

    private void OnMediaChanged(object? sender, MediaInfo media)
    {
        if (_mediaService.IsPlaying)
        {
            UpdatePresenceFromMedia(media);
        }
    }

    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        if (isPlaying && _mediaService.CurrentMedia != null)
        {
            UpdatePresenceFromMedia(_mediaService.CurrentMedia);
        }
        else
        {
            ClearPresence();
        }
    }

    private void UpdatePresenceFromMedia(MediaInfo media)
    {
        UpdatePresence(
            media.Title ?? "Unknown Track",
            media.Artist ?? "Unknown Artist",
            null, // Album art URL would need to be hosted somewhere
            null  // Could detect active game here
        );
    }

    public void UpdatePresence(string trackTitle, string artist, string? albumArt = null, string? game = null)
    {
        if (_client == null || !_client.IsInitialized)
            return;

        try
        {
            var presence = new RichPresence
            {
                Details = trackTitle.Length > 128 ? trackTitle[..125] + "..." : trackTitle,
                State = $"by {(artist.Length > 100 ? artist[..97] + "..." : artist)}",
                Assets = new Assets
                {
                    LargeImageKey = "tuneslayer_logo", // Default logo asset
                    LargeImageText = "TunesLayer",
                    SmallImageKey = GetSourceIcon(_mediaService.CurrentMedia?.SourceApp),
                    SmallImageText = _mediaService.CurrentMedia?.SourceApp ?? "Music"
                },
                Timestamps = new Timestamps
                {
                    Start = DateTime.UtcNow
                }
            };

            // Add game context if available
            if (!string.IsNullOrEmpty(game))
            {
                presence.State = $"by {artist} • Playing {game}";
            }

            _client.SetPresence(presence);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update Discord presence: {ex.Message}");
        }
    }

    private static string GetSourceIcon(string? sourceApp)
    {
        // These would need to be uploaded as assets in your Discord application
        return sourceApp?.ToLowerInvariant() switch
        {
            "spotify" => "spotify_icon",
            "apple music" => "apple_music_icon",
            "youtube music" => "youtube_music_icon",
            _ => "music_icon"
        };
    }

    public void ClearPresence()
    {
        _client?.ClearPresence();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _mediaService.MediaChanged -= OnMediaChanged;
        _mediaService.PlaybackStateChanged -= OnPlaybackStateChanged;

        _client?.ClearPresence();
        _client?.Dispose();
        _disposed = true;
    }
}
