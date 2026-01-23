using System.IO;
using System.Text.Json;
using TunesLayer.Core.Models;

namespace TunesLayer.Core.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TunesLayer");
    
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings CurrentSettings { get; private set; } = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = await File.ReadAllTextAsync(SettingsPath);
                CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            CurrentSettings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(CurrentSettings, JsonOptions);
            await File.WriteAllTextAsync(SettingsPath, json);
            SettingsChanged?.Invoke(this, CurrentSettings);
        }
        catch (Exception)
        {
            // Log error in production
        }
    }
}
