namespace TunesLayer.Integrations.Discord;

public interface IDiscordRpcService
{
    bool IsConnected { get; }
    void Initialize();
    void UpdatePresence(string trackTitle, string artist, string? albumArt = null, string? game = null);
    void ClearPresence();
    void Dispose();
}
